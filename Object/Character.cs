﻿using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using spikewall.Response;
using System.Text.Json.Serialization;

namespace spikewall.Object
{
    public class Character
    {
        // The internal ID for this character.
        public int characterId { get; set; }

        // The amount of rings this character
        // currently costs to level up.
        public ulong numRings { get; set; }

        // UNUSED: The amount of Red Star Rings
        // this character costs to level up.
        public ulong numRedRings { get; set; }

        // The amount of rings this
        // character costs to buy/limit smash.
        public ulong priceNumRings { get; set; }

        // The amount of Red Star Rings this
        // character costs to buy/limit smash.
        public ulong priceNumRedRings { get; set; }

        // Whether or not the character is unlocked.
        public sbyte status { get; set; }

        // The level of the character.
        public sbyte level { get; set; }

        // How many rings until
        // the next level up??
        public ulong exp { get; set; }

        // Amount of times the character
        // has been limit smashed.
        public sbyte star { get; set; }

        // Maximum amount of times the
        // character can be limit smashed.
        public sbyte starMax { get; set; }

        // How the character can be unlocked
        // (Purchasable with Red Rings, Winnable
        // on the Premium Roulette, etc).
        // TODO: Probably move this to an enum soon.
        public sbyte lockCondition { get; set; }

        // Not sure what this is right now.
        public Campaign[]? campaignList { get; set; }

        // The current levels for each ability.
        public long[] abilityLevel { get; set; }

        // Apparently this may be unused?
        // Otherwise, not sure what this is.
        public long[] abilityNumRings { get; set; }

        // The abilities that have been leveled
        // up using experience after a run.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long[] abilityLevelup { get; set; }

        // The amount of experience that was used to
        // level up the abilities in abilityLevelup.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ulong[]? abilityLevelupExp { get; set; }

        /// <summary>
        /// Enum that contains all of the
        /// possible states of a character.
        /// </summary>
        public enum Status
        {
            Locked,
            Unlocked,
            MaxLevel
        }

        /// <summary>
        /// Enum that contains all of the
        /// possible lock conditions of a character.
        /// </summary>
        public enum LockCondition
        {
            UnlockedByDefault,
            UnlockedAsMileageIncentive,
            UnlockedByPurchasing,
            UnlockedByRoulette
        }

        public static ulong GenerateTotalCost(MySqlConnection conn, int characterId, sbyte level)
        {
            ulong cost = 0;

            for (int lvl = 0; lvl <= level; lvl++)
            {
                var upgrdSql = Db.GetCommand("SELECT multiple FROM `sw_characterupgrades` WHERE character_id = '{0}' AND min_level <= '{1}' AND max_level >= '{1}';", characterId, lvl);
                var upgrdCmd = new MySqlCommand(upgrdSql, conn);

                ulong multiple = Convert.ToUInt64(upgrdCmd.ExecuteScalar());

                cost += multiple;
            }

            return cost;
        }

        public static SRStatusCode PopulateCharacterState(MySqlConnection conn, string uid, out Character[] characterState)
        {
            List<Character> characters = new List<Character>();
            characterState = null;

            // Get list of all visible characters
            var command = new MySqlCommand("SELECT * FROM `sw_characters` WHERE visible = '1';", conn);

            var charRdr = command.ExecuteReader();
            while (charRdr.Read())
            {
                Character c = new Character();

                c.characterId = charRdr.GetInt32("id");

                // FIXME: Hardcoded empty
                c.campaignList = new Campaign[0];

                c.numRings = Convert.ToUInt64(charRdr["num_rings"]);
                c.numRedRings = Convert.ToUInt64(charRdr["num_red_rings"]);
                c.priceNumRings = Convert.ToUInt64(charRdr["price_num_rings"]);
                c.priceNumRedRings = Convert.ToUInt64(charRdr["price_num_red_rings"]);
                c.starMax = Convert.ToSByte(charRdr["star_max"]);
                c.lockCondition = Convert.ToSByte(charRdr["lock_condition"]);

                characters.Add(c);
            }

            charRdr.Close();

            for (int i = 0; i < characters.Count; i++)
            {
                Character c = characters[i];

                var sql = Db.GetCommand("SELECT * FROM `sw_characterstates` WHERE user_id = '{0}' AND character_id = '{1}';", uid, c.characterId);
                var stateCmd = new MySqlCommand(sql, conn);
                var stateRdr = stateCmd.ExecuteReader();

                if (stateRdr.HasRows)
                {
                    // Read row
                    stateRdr.Read();

                    c.status = Convert.ToSByte(stateRdr["status"]);
                    c.level = Convert.ToSByte(stateRdr["level"]);
                    c.exp = Convert.ToUInt64(stateRdr["exp"]);
                    c.star = Convert.ToSByte(stateRdr["star"]);

                    c.abilityLevel = Db.ConvertDBListToIntArray(stateRdr.GetString("ability_level"));
                    c.abilityNumRings = Db.ConvertDBListToIntArray(stateRdr.GetString("ability_num_rings"));

                    stateRdr.Close();

                    // We calculate character prices dynamically
                    c.numRings = GenerateTotalCost(conn, c.characterId, c.level);
                }
                else
                {
                    // Player does not have a CharacterState yet, generate one
                    stateRdr.Close();

                    // Insert rows

                    c.status = (sbyte)((c.lockCondition != (sbyte)LockCondition.UnlockedByDefault) ? 0 : 1);
                    c.level = 0;
                    c.exp = 0;
                    c.star = 0;

                    var abilityLevelStr = "0 0 0 0 0 0 0 0 0 0 0";

                    c.abilityLevel = Db.ConvertDBListToIntArray(abilityLevelStr);
                    c.abilityNumRings = Db.ConvertDBListToIntArray(abilityLevelStr);

                    sql = Db.GetCommand(@"INSERT INTO `sw_characterstates` (
                                              user_id, character_id, status, level, exp, star, ability_level, ability_num_rings
                                          ) VALUES (
                                              '{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}'
                                          );", uid, c.characterId, c.status, c.level, c.exp, c.star, abilityLevelStr, abilityLevelStr);
                    var insertCmd = new MySqlCommand(sql, conn);
                    insertCmd.ExecuteNonQuery();
                }
            }

            conn.Close();

            characterState = characters.ToArray();
            return SRStatusCode.Ok;
        }

        public static SRStatusCode SaveCharacterState(MySqlConnection conn, string uid, Character[] characterState)
        {
            // FIXME: Does not support adding characters right now
            for (int i = 0; i < characterState.Length; i++)
            {
                var sql = Db.GetCommand(
                    @"UPDATE `sw_characterstates` SET
                    status = '{0}',
                    level = '{1}',
                    exp = '{2}',
                    star = '{3}',
                    ability_level = '{4}',
                    ability_num_rings = '{5}'
                  WHERE user_id = '{6}' AND character_id = '{7}';",
                        characterState[i].status,
                        characterState[i].level,
                        characterState[i].exp,
                        characterState[i].star,
                        Db.ConvertIntArrayToDBList(characterState[i].abilityLevel),
                        Db.ConvertIntArrayToDBList(characterState[i].abilityNumRings),
                        uid,
                        characterState[i].characterId);
                var command = new MySqlCommand(sql, conn);

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    // Failed to find row with this user ID
                    return SRStatusCode.MissingPlayer;
                }
            }

            return SRStatusCode.Ok;
        }

        public static int FindCharacterInCharacterState(int characterId, Character[] characterState)
        {
            int index = -1;
            for (int i = 0; i < characterState.Length; i++)
            {
                if (characterState[i].characterId == characterId)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        public static SRStatusCode LevelUpCharacterWithExp(MySqlConnection conn, int characterId, ulong exp, ref Character[] characterState, out int charaIndex)
        {
            // We need to find the index of the provided character in the CharacterState
            charaIndex = FindCharacterInCharacterState(characterId, characterState);

            if (charaIndex == -1)
            {
                // The character we want to upgrade isn't available to the player, abort
                return SRStatusCode.InternalServerError;
            }

            if (characterState[charaIndex].level < 100)
            {
                characterState[charaIndex].exp += exp;

                List<long> abilityLevelup = new();
                List<ulong> abilityLevelupExp = new();

                int abilityIndex;

                Random random = new();

                conn.Open();

                while (characterState[charaIndex].exp >= characterState[charaIndex].numRings && characterState[charaIndex].level < 100)
                {
                    // Make sure we're only leveling up abilities that aren't maxed out
                    do
                    {
                        abilityIndex = random.Next(0, 10);
                    } while (characterState[charaIndex].abilityLevel[abilityIndex] == 10);

                    characterState[charaIndex].level++;
                    characterState[charaIndex].abilityLevel[abilityIndex]++;
                    abilityLevelup.Add(120000 + abilityIndex);
                    abilityLevelupExp.Add(characterState[charaIndex].numRings);
                    characterState[charaIndex].exp -= characterState[charaIndex].numRings;

                    characterState[charaIndex].numRings = GenerateTotalCost(conn, characterId, characterState[charaIndex].level);
                }

                characterState[charaIndex].abilityLevelup = abilityLevelup.ToArray();
                characterState[charaIndex].abilityLevelupExp = abilityLevelupExp.ToArray();
            }
            // Character hit level 100, set them to max level
            else characterState[charaIndex].status = (sbyte)Status.MaxLevel;

            return SRStatusCode.Ok;
        }
    }
}
