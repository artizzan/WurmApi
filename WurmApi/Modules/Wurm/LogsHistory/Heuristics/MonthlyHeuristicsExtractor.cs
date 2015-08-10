﻿using System;
using AldursLab.Essentials;
using AldursLab.WurmApi.Modules.Wurm.LogsHistory.Heuristics.MonthlyDataBuilders;
using AldursLab.WurmApi.Utility;

namespace AldursLab.WurmApi.Modules.Wurm.LogsHistory.Heuristics
{
    /// <summary>
    /// Parses monthly log file to extract day heuristics, useful to learn, 
    /// where each day block starts and how long it is. 
    /// Read remarks for exact behavior.
    /// </summary>
    /// <remarks>
    /// For any non-current month log file, it will extract a full set of info,
    /// one record per each day. For current month log files, data is extracted
    /// only up to current day and current day has arbitrarily high line count,
    /// indicating it is not known yet.
    /// 
    /// WurmApiException is thrown on extraction, if:
    /// - parsed file has format not of standard monthly Wurm Online log file.
    /// - file is empty
    /// - file has fully unrecognizable data
    /// Exception will not be thrown on occasional malformed lines, 
    /// however these events are logged. Malformed lines will not affect heuristics
    /// reliability.
    /// 
    /// Final actual log day (and days after it) start position may not be exact.
    /// It might be early, to be precise. Anything relying on this data, must be prepared
    /// to read an unexpected line, for example empty or containing single character.
    /// </remarks>
    public class MonthlyHeuristicsExtractor
    {
        private readonly LogFileInfo logFileInfo;
        private readonly LogFileStreamReaderFactory logFileStreamReaderFactory;
        private readonly ILogger logger;

        public MonthlyHeuristicsExtractor(
            LogFileInfo logFileInfo, 
            LogFileStreamReaderFactory logFileStreamReaderFactory,
            ILogger logger)
        {
            if (logFileInfo == null) throw new ArgumentNullException("logFileInfo");
            if (logFileStreamReaderFactory == null) throw new ArgumentNullException("logFileStreamReaderFactory");
            if (logger == null) throw new ArgumentNullException("logger");
            this.logFileInfo = logFileInfo;
            this.logFileStreamReaderFactory = logFileStreamReaderFactory;
            this.logger = logger;
        }

        public HeuristicsExtractionResult ExtractDayToPositionMap()
        {
            using (var reader = logFileStreamReaderFactory.Create(logFileInfo.FullPath, startPosition: 0, trackFileBytePositions: true))
            {
                string line;
                IMonthlyHeuristicsDataBuilder builder = new DataBuilderV2(logFileInfo.FileName, Time.Get.LocalNow, logger);

                while ((line = reader.TryReadNextLine()) != null)
                {
                    builder.ProcessLine(line, reader.LastReadLineStartPosition);
                }
                builder.Complete(reader.LastReadLineStartPosition);
                var result = builder.GetResult();
                return result;
            }
        }

        public HeuristicsExtractionResult ExtractDayToPositionMapAsync()
        {
            return ExtractDayToPositionMap();
        }
    }
}