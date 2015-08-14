﻿using System;

namespace AldursLab.WurmApi.PersistentObjects
{
    abstract class PersistentEntityBase<TEntity> where TEntity : Entity, new()
    {
        readonly IPersistent<TEntity> persistent;

        public PersistentEntityBase(IPersistent<TEntity> persistent) 
        {
            if (persistent == null) throw new ArgumentNullException("persistent");
            this.persistent = persistent;
        }

        protected TEntity Entity { get { return persistent.Entity; } }

        protected void FlagAsChanged()
        {
            persistent.FlagAsChanged();
        }

        protected void RunMigration(int sourceVersion, int targetVersion, Action<TEntity> migrationAction, Func<TEntity, bool> entityFilteringPredicate = null)
        {
            if (migrationAction == null)
                throw new ArgumentNullException("migrationAction");
            if (entityFilteringPredicate != null)
            {
                if (!entityFilteringPredicate(persistent.Entity))
                {
                    return;
                }
            }
            if (persistent.Entity.Version == sourceVersion)
            {
                migrationAction(persistent.Entity);
                persistent.Entity.Version = targetVersion;
                FlagAsChanged();
            }
        }
    }
}
