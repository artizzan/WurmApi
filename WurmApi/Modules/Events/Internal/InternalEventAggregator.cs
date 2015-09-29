using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AldursLab.WurmApi.Modules.Events.Internal.Messages;

namespace AldursLab.WurmApi.Modules.Events.Internal
{
    /// <summary>
    ///   Enables loosely-coupled publication of and subscription to events.
    /// </summary>
    class InternalEventAggregator : IInternalEventAggregator 
    {
        readonly List<Handler> handlers = new List<Handler>();

        /// <summary>
        ///   The default thread marshaller used for publication;
        /// </summary>
        public static Action<System.Action> DefaultPublicationThreadMarshaller = action => action();

        /// <summary>
        /// Processing of handler results on publication thread.
        /// </summary>
        public static Action<object, object> HandlerResultProcessing = (target, result) => { };

        /// <summary>
        ///   Initializes a new instance of the <see cref = "InternalEventAggregator" /> class.
        /// </summary>
        public InternalEventAggregator() 
        {
            PublicationThreadMarshaller = DefaultPublicationThreadMarshaller;
        }

        /// <summary>
        ///   Gets or sets the default publication thread marshaller.
        /// </summary>
        /// <value>
        ///   The default publication thread marshaller.
        /// </value>
        public Action<System.Action> PublicationThreadMarshaller { get; set; }

        /// <summary>
        /// Searches the subscribed handlers to check if we have a handler for
        /// the message type supplied.
        /// </summary>
        /// <param name="messageType">The message type to check with</param>
        /// <returns>True if any handler is found, false if not.</returns>
        public bool HandlerExistsFor(Type messageType) 
        {
            return handlers.Any(handler => handler.Handles(messageType) & !handler.IsDead);
        }

        /// <summary>
        ///   Subscribes an instance to all events declared through implementations of <see cref = "IHandle{TMessage}" />
        /// </summary>
        /// <param name = "subscriber">The instance to subscribe for event publication.</param>
        public virtual void Subscribe(object subscriber) 
        {
            if (subscriber == null) {
                throw new ArgumentNullException("subscriber");
            }
            lock(handlers) {
                if (handlers.Any(x => x.Matches(subscriber))) {
                    return;
                }

                handlers.Add(new Handler(subscriber));
            }
        }

        /// <summary>
        ///   Unsubscribes the instance from all events.
        /// </summary>
        /// <param name = "subscriber">The instance to unsubscribe.</param>
        public virtual void Unsubscribe(object subscriber) 
        {
            if (subscriber == null) {
                throw new ArgumentNullException("subscriber");
            }
            lock(handlers) {
                var found = handlers.FirstOrDefault(x => x.Matches(subscriber));

                if (found != null) {
                    handlers.Remove(found);
                }
            }
        }

        /// <summary>
        ///   Publishes a message.
        /// </summary>
        /// <param name = "message">The message instance.</param>
        /// <remarks>
        ///   Does not marshall the the publication to any special thread by default.
        /// </remarks>
        public virtual void Send(Message message) 
        {
            if (message == null) {
                throw new ArgumentNullException("message");
            }
            Send(message, PublicationThreadMarshaller);
        }

        /// <summary>
        ///   Publishes a message.
        /// </summary>
        /// <param name = "message">The message instance.</param>
        /// <param name = "marshal">Allows the publisher to provide a custom thread marshaller for the message publication.</param>
        public virtual void Send(Message message, Action<System.Action> marshal)
        {
            if (message == null){
                throw new ArgumentNullException("message");
            }
            if (marshal == null) {
                throw new ArgumentNullException("marshal");
            }

            Handler[] toNotify;
            lock (handlers) {
                toNotify = handlers.ToArray();
            }

            marshal(() => {
                              var messageType = message.GetType();

                              var dead = toNotify
                                  .Where(handler => !handler.Handle(messageType, message))
                                  .ToList();

                              if(dead.Any()) {
                                  lock(handlers) {
                                      dead.Apply(x => handlers.Remove(x));
                                  }
                              }
            });
        }

        class Handler {
            readonly WeakReference reference;
            readonly Dictionary<Type, MethodInfo> supportedHandlers = new Dictionary<Type, MethodInfo>();

            public bool IsDead {
                get { return reference.Target == null; }
            }

            public Handler(object handler) {
                reference = new WeakReference(handler);

#if WinRT
                var handlerInfo = typeof(IHandle).GetTypeInfo();
                var interfaces = handler.GetType().GetTypeInfo().ImplementedInterfaces
                    .Where(x => handlerInfo.IsAssignableFrom(x.GetTypeInfo()) && x.GetTypeInfo().IsGenericType);

                foreach (var @interface in interfaces) {
                    var type = @interface.GenericTypeArguments[0];
                    var method = @interface.GetTypeInfo().DeclaredMethods.First(x => x.Name == "Handle");
                    supportedHandlers[type] = method;
                }
#else
                var interfaces = handler.GetType().GetInterfaces()
                                        .Where(x => typeof(IHandle).IsAssignableFrom(x) && x.IsGenericType);

                foreach(var @interface in interfaces) {
                    var type = @interface.GetGenericArguments()[0];
                    var method = @interface.GetMethod("Handle");
                    supportedHandlers[type] = method;
                }
#endif
            }

            public bool Matches(object instance) {
                return reference.Target == instance;
            }

            public bool Handle(Type messageType, object message) {
                var target = reference.Target;
                if (target == null) {
                    return false;
                }

                foreach(var pair in supportedHandlers) {
                    if(pair.Key.IsAssignableFrom(messageType)) {
                        var result = pair.Value.Invoke(target, new[] { message });
                        if (result != null) {
                            HandlerResultProcessing(target, result);
                        }
                    }
                }
                
                return true;
            }

            public bool Handles(Type messageType) {
                return supportedHandlers.Any(pair => pair.Key.IsAssignableFrom(messageType));
            }
        }
    }
}