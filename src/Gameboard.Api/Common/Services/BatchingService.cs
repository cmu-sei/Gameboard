using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Common.Services;

public interface IBatchingService
{
    /// <summary>
    /// Create batches from the input enumerable of maximum size equal to the number of 
    /// batches. Note that the last batch may have fewer items than the batch size
    /// if the length of the items enumerable doesn't divide evenly over the
    /// number of batches.
    /// 
    /// </summary>
    /// <param name="items"></param>
    /// <param name="batchSize"></param>
    /// <returns></returns>
    IEnumerable<IEnumerable<T>> Batch<T>(IEnumerable<T> items, int batchSize);
}

internal class BatchingService : IBatchingService
{
    private readonly ILogger<BatchingService> _logger;

    public BatchingService(ILogger<BatchingService> logger)
    {
        _logger = logger;
    }

    public IEnumerable<IEnumerable<T>> Batch<T>(IEnumerable<T> items, int batchSize)
    {
        var itemCount = items.Count();

        if (batchSize <= 0)
            throw new ArgumentException($"{nameof(batchSize)} must be a positive value.", nameof(batchSize));

        if (itemCount == 0)
            throw new ArgumentException($"{nameof(items)} contains no items to batch.", nameof(items));

        // if the number of batches is 1, return n batches of 1 where n is number of batches
        if (batchSize == 1)
        {
            _logger.LogInformation(message: $"Building batches of size 1 for {items.Count()} items.");
            return items.Select(i => new T[] { i }).ToArray();
        }

        // otherwise, create batches o fthe appropriate size plus a potential additional batch for any leftovers
        var batchList = new List<IEnumerable<T>>();
        List<T> currentBatch = null;
        // resolve the enumerable of challenges to an array for index access
        var itemsArray = items.ToArray();

        for (var itemIndex = 0; itemIndex < itemCount; itemIndex++)
        {

            if (itemIndex % batchSize == 0)
            {
                currentBatch = new List<T>();
                batchList.Add(currentBatch);
            }

            currentBatch.Add(itemsArray[itemIndex]);
        }

        return batchList;
    }
}


// public interface IExternalGameDeployBatchService
// {
//     /// <summary>
//     /// Create batches of gamespace deploy requests from challenges. 
//     /// 
//     /// An external game can have many challenges, and each challenge has an associated gamespace. For
//     /// each gamespace, Gameboard must issue a request to the game engine that causes it to deploy
//     /// the gamespace. Requesting all of these at once can cause issues with request timeouts, so
//     /// we optionally allow Gameboard sysadmins to configure a batch size appropriate to their game engine.
//     /// 
//     /// By default, this value is 4, meaning that upon start of an external game, Gameboard will issue
//     /// requests to the game engine in batches of 4 until all gamespaces have been deployed. Configure this
//     /// with the Core__GameEngineDeployBatchSize setting in Gameboard's helm chart.
//     /// </summary>
//     /// <param name="challenges"></param>
//     /// <returns></returns>
//     IEnumerable<IEnumerable<GameStartContextChallenge>> BuildDeployBatches(IEnumerable<GameStartContextChallenge> challenges);
// }

// internal class ExternalGameDeployBatchService : IExternalGameDeployBatchService
// {
//     private readonly CoreOptions _coreOptions;
//     private readonly ILogger<ExternalGameDeployBatchService> _logger;

//     public ExternalGameDeployBatchService
//     (
//         CoreOptions coreOptions,
//         ILogger<ExternalGameDeployBatchService> logger
//     )
//     {
//         _coreOptions = coreOptions;
//         _logger = logger;
//     }

//     public IEnumerable<IEnumerable<GameStartContextChallenge>> BuildDeployBatches(IEnumerable<GameStartContextChallenge> challenges)
//     {
//         // if the setting isn't configured or is a nonsense value, just return one batch for each task (a synchronous
//         // deploy, in effect)
//         if (_coreOptions.GameEngineDeployBatchSize <= 1)
//         {
//             _logger.LogInformation(message: $"No explicit batch size configured. Building a single batch with {challenges.Count()} challenges.");
//             return challenges.Select(c => new GameStartContextChallenge[] { c }).ToArray();
//         }

//         // otherwise, create batches of the appropriate size plus an additional batch for any leftovers
//         var batchList = new List<IEnumerable<GameStartContextChallenge>>();
//         List<GameStartContextChallenge> currentBatch = null;
//         // resolve the enumerable of challenges to an array for index access
//         var challengesArray = challenges.ToArray();

//         for (var challengeIndex = 0; challengeIndex < challengesArray.Length; challengeIndex++)
//         {
//             if (challengeIndex % _coreOptions.GameEngineDeployBatchSize == 0)
//             {
//                 currentBatch = new List<GameStartContextChallenge>();
//                 batchList.Add(currentBatch);
//             }

//             currentBatch.Add(challengesArray[challengeIndex]);
//         }

//         return batchList;
//     }
// }
