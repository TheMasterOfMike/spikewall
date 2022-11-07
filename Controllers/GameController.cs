﻿using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using spikewall.Debug;
using spikewall.Encryption;
using spikewall.Object;
using spikewall.Request;
using spikewall.Response;

namespace spikewall.Controllers
{
    [ApiController]
    public class GameController : ControllerBase
    {
        [HttpPost]
        [Route("/Game/getDailyChalData/")]
        [Produces("text/json")]
        public JsonResult GetDailyChalData([FromForm] string param, [FromForm] string secure, [FromForm] string key = "")
        {
            var iv = (string)Config.Get("encryption_iv");

            using var conn = Db.Get();
            conn.Open();

            var clientReq = new ClientRequest<BaseRequest>(conn, param, secure, key);
            if (clientReq.error != SRStatusCode.Ok)
            {
                return new JsonResult(EncryptedResponse.Generate(iv, clientReq.error));
            }

            var sql = Db.GetCommand("SELECT * FROM `sw_dailychallenge`");
            var command = new MySqlCommand(sql, conn);
            var reader = command.ExecuteReader();

            DailyChalDataResponse dailyChalDataResponse = new();

            if (reader.Read())
            {
                Incentive[] incentives = new Incentive[7];

                for (int i = 0; i < 7; i++)
                {
                    incentives[i] = new Incentive();
                    incentives[i].itemId = reader.GetInt64("item" + (i + 1));
                    incentives[i].numItem = reader.GetInt64("item" + (i + 1) + "_count");
                    incentives[i].numIncentiveCont = i + 1;
                }

                reader.Close();

                PlayerState playerState = new();
                playerState.Populate(conn, clientReq.userId);

                dailyChalDataResponse.incentiveList = incentives;
                dailyChalDataResponse.incentiveListCont = 7;
                dailyChalDataResponse.numDilayChalCont = playerState.numDailyChalCont;
                dailyChalDataResponse.maxDailyChalDay = 7;
                dailyChalDataResponse.numDailyChalDay = 1; // FIXME: Hardcoded
                dailyChalDataResponse.chalEndTime = DateTimeOffset.Now.AddDays(1).AddTicks(-1).ToUnixTimeSeconds(); // FIXME: This should be the end of the day
            }
            else dailyChalDataResponse.incentiveList = Array.Empty<Incentive>();

            return new JsonResult(EncryptedResponse.Generate(iv, dailyChalDataResponse));
        }

        [HttpPost]
        [Route("/Game/getCostList/")]
        [Produces("text/json")]
        public JsonResult GetCostList([FromForm] string param, [FromForm] string secure, [FromForm] string key = "")
        {
            var iv = (string)Config.Get("encryption_iv");

            using var conn = Db.Get();
            conn.Open();

            var clientReq = new ClientRequest<BaseRequest>(conn, param, secure, key);
            if (clientReq.error != SRStatusCode.Ok)
            {
                return new JsonResult(EncryptedResponse.Generate(iv, clientReq.error));
            }

            var sql = Db.GetCommand("SELECT * FROM `sw_costlist`");
            var command = new MySqlCommand(sql, conn);
            var reader = command.ExecuteReader();

            CostListResponse costListResponse = new();

            if (reader.Read())
            {
                ConsumedItem[] consumedItems = new ConsumedItem[15];

                for (int i = 0; i < 15; i++)
                {
                    consumedItems[i] = new ConsumedItem();
                    consumedItems[i].itemId = reader.GetInt64("item" + (i + 1));
                    consumedItems[i].numItem = reader.GetInt64("item" + (i + 1) + "_cost");
                    consumedItems[i].consumedItemId = reader.GetInt64("item" + (i + 1) + "_id");
                }

                reader.Close();

                costListResponse.consumedCostList = consumedItems;
            }
            else costListResponse.consumedCostList = Array.Empty<ConsumedItem>();

            return new JsonResult(EncryptedResponse.Generate(iv, costListResponse));
        }

        [HttpPost]
        [Route("/Game/getMileageData/")]
        [Produces("text/json")]
        public JsonResult GetMileageData([FromForm] string param, [FromForm] string secure, [FromForm] string key = "")
        {
            var iv = (string)Config.Get("encryption_iv");

            using var conn = Db.Get();
            conn.Open();

            var clientReq = new ClientRequest<BaseRequest>(conn, param, secure, key);
            if (clientReq.error != SRStatusCode.Ok)
            {
                return new JsonResult(EncryptedResponse.Generate(iv, clientReq.error));
            }

            // FIXME: Stub

            return new JsonResult(EncryptedResponse.Generate(iv, new MileageDataResponse()));
        }

        [HttpPost]
        [Route("/Game/getCampaignList/")]
        [Produces("text/json")]
        public JsonResult GetCampaignList([FromForm] string param, [FromForm] string secure, [FromForm] string key = "")
        {
            var iv = (string)Config.Get("encryption_iv");

            using var conn = Db.Get();
            conn.Open();

            var clientReq = new ClientRequest<BaseRequest>(conn, param, secure, key);
            if (clientReq.error != SRStatusCode.Ok)
            {
                return new JsonResult(EncryptedResponse.Generate(iv, clientReq.error));
            }

            // FIXME: Stub

            return new JsonResult(EncryptedResponse.Generate(iv, new CampaignListResponse()));
        }

        [HttpPost]
        [Route("/Game/getFreeItemList/")]
        [Produces("text/json")]
        public JsonResult GetFreeItemList([FromForm] string param, [FromForm] string secure, [FromForm] string key = "")
        {
            var iv = (string)Config.Get("encryption_iv");

            using var conn = Db.Get();
            conn.Open();

            var clientReq = new ClientRequest<BaseRequest>(conn, param, secure, key);
            if (clientReq.error != SRStatusCode.Ok)
            {
                return new JsonResult(EncryptedResponse.Generate(iv, clientReq.error));
            }

            // FIXME: Stub

            return new JsonResult(EncryptedResponse.Generate(iv, new FreeItemListResponse()));
        }

        [HttpPost]
        [Route("/Game/quickActStart/")]
        [Produces("text/json")]
        public JsonResult QuickActStart([FromForm] string param, [FromForm] string secure, [FromForm] string key = "")
        {
            var iv = (string)Config.Get("encryption_iv");
            BaseResponse error = null;

            // FIXME: Actually do something with this information
            using var conn = Db.Get();
            conn.Open();

            var clientReq = new ClientRequest<QuickActStartRequest>(conn, param, secure, key);
            if (clientReq.error != SRStatusCode.Ok)
            {
                return new JsonResult(EncryptedResponse.Generate(iv, clientReq.error));
            }

            QuickActStartResponse quickActStartBaseResponse = new();

            // Now that we have the user ID, we can retrieve the player state
            PlayerState playerState = new PlayerState();

            var populateStatus = playerState.Populate(conn, clientReq.userId);

            conn.Close();

            if (populateStatus == SRStatusCode.Ok)
            {
                quickActStartBaseResponse.playerState = playerState;
                return new JsonResult(EncryptedResponse.Generate(iv, quickActStartBaseResponse));
            }
            else
            {
                // Return error code from Populate() to client
                return new JsonResult(EncryptedResponse.Generate(iv, new BaseResponse(populateStatus)));
            }
        }

        [HttpPost]
        [Route("/Game/quickPostGameResults/")]
        [Produces("text/json")]
        public JsonResult QuickPostGameResults([FromForm] string param, [FromForm] string secure, [FromForm] string key = "")
        {
            var iv = (string)Config.Get("encryption_iv");
            BaseResponse error = null;

            using var conn = Db.Get();
            conn.Open();

            var clientReq = new ClientRequest<QuickPostGameResultsRequest>(conn, param, secure, key);
            if (clientReq.error != SRStatusCode.Ok)
            {
                return new JsonResult(EncryptedResponse.Generate(iv, clientReq.error));
            }

            var request = clientReq.request;

            // Now that we have the user ID, we can retrieve the player state
            PlayerState playerState = new PlayerState();

            var populateStatus = playerState.Populate(conn, clientReq.userId);
            if (populateStatus != SRStatusCode.Ok)
            {
                return new JsonResult(EncryptedResponse.Generate(iv, populateStatus));
            }

            var score = ulong.Parse(request.score);
            var animals = ulong.Parse(request.numAnimals);
            var rings = ulong.Parse(request.numRings);
            var redStarRings = ulong.Parse(request.numRedStarRings);
            var distance = ulong.Parse(request.distance);

            if (playerState.quickTotalHighScore < score)
            {
                playerState.quickTotalHighScore = score;
            }

            playerState.numAnimals += animals;
            playerState.numRings += rings;
            playerState.numRedRings += redStarRings;
            playerState.totalDistance += distance;

            var saveStatus = playerState.Save(conn, clientReq.userId);
            if (saveStatus != SRStatusCode.Ok)
            {
                return new JsonResult(EncryptedResponse.Generate(iv, saveStatus));
            }

            conn.Close();

            QuickPostGameResultsResponse quickPostGameResultsResponse = new();
            quickPostGameResultsResponse.playerState = playerState;

            // FIXME: Actually implement this normally lmao

            quickPostGameResultsResponse.dailyChallengeIncentive = new Incentive[0];
            quickPostGameResultsResponse.messageList = new string[0];
            quickPostGameResultsResponse.operatorMessageList = new string[0];
            quickPostGameResultsResponse.totalMessage = 0;
            quickPostGameResultsResponse.totalOperatorMessage = 0;

            return new JsonResult(EncryptedResponse.Generate(iv, quickPostGameResultsResponse));
        }

        /// <summary>
        /// Endpoint hit when a player revives using Red Star Rings.
        /// </summary>
        [HttpPost]
        [Route("/Game/actRetry/")]
        [Produces("text/json")]
        public JsonResult ActRetry([FromForm] string param, [FromForm] string secure, [FromForm] string key = "")
        {
            var iv = (string)Config.Get("encryption_iv");
            BaseResponse error = null;

            using var conn = Db.Get();
            conn.Open();

            var clientReq = new ClientRequest<BaseRequest>(conn, param, secure, key);
            if (clientReq.error != SRStatusCode.Ok)
            {
                return new JsonResult(EncryptedResponse.Generate(iv, clientReq.error));
            }

            // Now that we have the user ID, we can retrieve the player state
            PlayerState playerState = new PlayerState();

            var populateStatus = playerState.Populate(conn, clientReq.userId);
            if (populateStatus != SRStatusCode.Ok)
            {
                return new JsonResult(EncryptedResponse.Generate(iv, populateStatus));
            }

            ulong reviveCost = (ulong)Config.Get("revive_rsr_cost");

            if (playerState.numRedRings >= reviveCost)
            {
                playerState.numRedRings -= reviveCost;

                var saveStatus = playerState.Save(conn, clientReq.userId);
                if (saveStatus != SRStatusCode.Ok)
                {
                    return new JsonResult(EncryptedResponse.Generate(iv, saveStatus));
                }
                conn.Close();
                return new JsonResult(EncryptedResponse.Generate(iv, new BaseResponse()));
            }
            else
            {
                conn.Close();
                return new JsonResult(EncryptedResponse.Generate(iv, new BaseResponse(SRStatusCode.NotEnoughRedStarRings)));
            }
        }
    }
}
