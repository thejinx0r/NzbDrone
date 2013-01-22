﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Core.Model;
using NzbDrone.Core.Model.Notification;
using NzbDrone.Core.Providers.DecisionEngine;
using NzbDrone.Core.Repository;
using NzbDrone.Core.Repository.Search;

namespace NzbDrone.Core.Providers.Search
{
    public class AnimeEpisodeSearch : SearchBase
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public AnimeEpisodeSearch(SeriesProvider seriesProvider, EpisodeProvider episodeProvider, DownloadProvider downloadProvider, IndexerProvider indexerProvider,
                             SceneMappingProvider sceneMappingProvider, AllowedDownloadSpecification allowedDownloadSpecification,
                             SearchHistoryProvider searchHistoryProvider)
                        : base(seriesProvider, episodeProvider, downloadProvider, indexerProvider, sceneMappingProvider, 
                               allowedDownloadSpecification, searchHistoryProvider)
            {
        }

        public AnimeEpisodeSearch()
        {
        }

        public override List<EpisodeParseResult> PerformSearch(Series series, dynamic options, ProgressNotification notification)
        {
            if (options.Episode == null)
                throw new ArgumentException("Episode is invalid");

            notification.CurrentMessage = "Looking for " + options.Episode;

            Episode episode = options.Episode;
            var reports = new List<EpisodeParseResult>();
            var title = GetSearchTitle(series, episode.SeasonNumber);

            var absoluteEpisodeNumber = episode.AbsoluteEpisodeNumber;

            if(series.UseSceneNumbering && episode.SceneAbsoluteEpisodeNumber > 0)
            {
                logger.Trace("Using SceneAbsoluteEpisodeNumber for {0}", episode);
                absoluteEpisodeNumber = episode.SceneAbsoluteEpisodeNumber;
            }

            Parallel.ForEach(_indexerProvider.GetEnabledIndexers(), indexer =>
            {
                try
                {
                    reports.AddRange(indexer.FetchAnime(title, absoluteEpisodeNumber));
                }

                catch (Exception e)
                {
                    logger.ErrorException(String.Format("An error has occurred while searching for {0}-{1:00} from: {2}",
                                                         series.Title, episode.AbsoluteEpisodeNumber, indexer.Name), e);
                }
            });

            return reports;
        }

        public override SearchHistoryItem CheckReport(Series series, dynamic options, EpisodeParseResult episodeParseResult,
                                                                SearchHistoryItem item)
        {
            if (episodeParseResult.AbsoluteEpisodeNumbers == null || !episodeParseResult.AbsoluteEpisodeNumbers.Any())
            {
                logger.Trace("No absolute episode numbers were found in post, skipping.");
                item.SearchError = ReportRejectionType.WrongEpisode;
                return item;
            }

            Episode episode = options.Episode;

            if (series.UseSceneNumbering && episode.SceneAbsoluteEpisodeNumber > 0)
            {
                if (!episodeParseResult.AbsoluteEpisodeNumbers.Contains(options.Episode.SceneAbsoluteEpisodeNumber))
                {
                    logger.Trace("Searched episode number is not contained in post, skipping.");
                    item.SearchError = ReportRejectionType.WrongEpisode;
                    return item;
                }
            }

            if (!episodeParseResult.AbsoluteEpisodeNumbers.Contains(options.Episode.AbsoluteEpisodeNumber))
            {
                logger.Trace("Searched episode number is not contained in post, skipping.");
                item.SearchError = ReportRejectionType.WrongEpisode;
                return item;
            }

            return item;
        }

        protected override void FinalizeSearch(Series series, dynamic options, Boolean reportsFound, ProgressNotification notification)
        {
            logger.Warn("Unable to find {0} in any of indexers.", options.Episode);

            notification.CurrentMessage = reportsFound ? String.Format("Sorry, couldn't find {0}, that matches your preferences.", options.Episode)
                                                        : String.Format("Sorry, couldn't find {0} in any of indexers.", options.Episode);
        }
    }
}