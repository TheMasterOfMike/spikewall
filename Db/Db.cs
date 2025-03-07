using MySql.Data;
using MySql.Data.MySqlClient;
using System.Text;

namespace spikewall
{
    public class Db
    {
        /// <summary>
        /// Initialize database details from locally-stored secrets
        /// </summary>
        /// MySQL connection details are stored as .NET secrets. To set these on the server (or
        /// locally), issue the following commands (replacing the second value as necessary):
        ///
        ///   dotnet user-secrets set "Db:Host" "localhost"
        ///   dotnet user-secrets set "Db:Port" "3306"
        ///   dotnet user-secrets set "Db:Username" "dbuser"
        ///   dotnet user-secrets set "Db:Password" "dbpass"
        ///   dotnet user-secrets set "Db:Database" "spikewall"
        ///
        public static void Initialize(ref WebApplicationBuilder builder)
        {
            m_dbHost = builder.Configuration["Db:Host"] ?? throw new InvalidOperationException("The \"Db:Host\" secret cannot be null. Did you make sure you set up the secrets properly?");
            m_dbUser = builder.Configuration["Db:Username"] ?? throw new InvalidOperationException("The \"Db:Username\" secret cannot be null. Did you make sure you set up the secrets properly?");
            m_dbPass = builder.Configuration["Db:Password"] ?? throw new InvalidOperationException("The \"Db:Password\" secret cannot be null. Did you make sure you set up the secrets properly?");
            m_dbName = builder.Configuration["Db:Database"] ?? throw new InvalidOperationException("The \"Db:Database\" secret cannot be null. Did you make sure you set up the secrets properly?");

            try
            {
                m_dbPort = short.Parse(builder.Configuration["Db:Port"] ?? throw new InvalidOperationException("The \"Db:Port\" secret cannot be null. Did you make sure you set up the secrets properly?"));
            }
            catch (ArgumentNullException)
            {
                m_dbPort = 0;
            }
        }

        /// <summary>
        /// Retrieve valid MySQL connection to make queries with.
        /// </summary>
        ///
        /// This function will return a valid MySqlConnection, or pass a MySqlException if an error
        /// occurs. Using try/catch is recommended, at least in sections where the validity of
        /// database details is checked.
        ///
        /// This can also be used in a `using` statement, e.g. `using (var conn = Db.Get())`
        ///
        /// After calling this function, call `Open()` on the resulting object. Then queries can be
        /// made with MySqlCommand. Call `Close()` when you're finished (this might happen
        /// automatically with `using`, but it's probably good practice either way),
        public static MySqlConnection Get()
        {
            // Build MySQL connection string out of loaded parameters
            var connectionString =
                $"server={m_dbHost};user={m_dbUser};database={m_dbName};port={m_dbPort};password={m_dbPass}";

            // Return connection
            return new MySqlConnection(connectionString);
        }

        public static string EscapeString(string s)
        {
            return s.Replace("'", "\\'");
        }

        /// <summary>
        /// Generate an SQL string where all paramters are escaped (assumes single quotes are used for values)
        /// </summary>
        public static string GetCommand(string format, params object[] arg)
        {
            for (var i = 0; i < arg.Length; i++) {
                if (arg[i] is string) {
                    arg[i] = EscapeString((string) arg[i]);
                }
            }
            return string.Format(format, arg);
        }

        public static long[] ConvertDBListToIntArray(string s)
        {
            var tokens = s.Split(' ');
            var values = new long[tokens.Length];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = long.Parse(tokens[i]);
            }
            return values;
        }

        public static string ConvertIntArrayToDBList(IEnumerable<long> a)
        {
            StringBuilder dbList = new();
            dbList.AppendJoin(' ', a);

            return dbList.ToString();
        }

        private static void QuickRun(MySqlConnection conn, string query)
        {
            var cmd = new MySqlCommand(query, conn);
            cmd.ExecuteNonQuery();
        }

        public static void ResetDatabase(bool chao = false,
                                         bool players = false,
                                         bool characters = false,
                                         bool mileageMapStates = false,
                                         bool config = false,
                                         bool tickers = false,
                                         bool dailyChallenge = false,
                                         bool costs = false,
                                         bool information = false)
        {
            using var conn = Db.Get();
            conn.Open();

            // Drop and recreate chao and chaostates
            if (chao)
            {
                QuickRun(conn,
                    @"DROP TABLE IF EXISTS `sw_chao`;
                    DROP TABLE IF EXISTS `sw_chaostates`;
                    CREATE TABLE `sw_chao` (
                        id MEDIUMINT UNSIGNED NOT NULL PRIMARY KEY,
                        rarity INTEGER NOT NULL DEFAULT 0,
                        hidden TINYINT NOT NULL DEFAULT 0
                    );
                    INSERT INTO `sw_chao` (id) VALUES ('400000');
                    CREATE TABLE `sw_chaostates` (
                        chao_id MEDIUMINT UNSIGNED NOT NULL,
                        user_id BIGINT UNSIGNED NOT NULL,
                        status TINYINT NOT NULL DEFAULT 0,
                        level INTEGER UNSIGNED NOT NULL DEFAULT 0,
                        set_status TINYINT NOT NULL DEFAULT 0,
                        acquired TINYINT NOT NULL DEFAULT 0
                    );");
            }

            // Drop and recreate players and sessions
            if (players)
            {
                QuickRun(conn,
                    @"DROP TABLE IF EXISTS `sw_players`;
                    DROP TABLE IF EXISTS `sw_sessions`;
                    DROP TABLE IF EXISTS `sw_itemownership`;
                    CREATE TABLE `sw_players` (
                        id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
                        password VARCHAR(20) NOT NULL,
                        server_key VARCHAR(20) NOT NULL,

                        username VARCHAR(12) NOT NULL DEFAULT '',
                        migrate_password VARCHAR(12),
                        language INTEGER,
                        suspended_until BIGINT,
                        suspend_reason INTEGER,

                        last_login BIGINT,
                        last_login_device TEXT,
                        last_login_platform INTEGER,
                        last_login_version TEXT,

                        main_chara_id MEDIUMINT NOT NULL DEFAULT 300000,
                        sub_chara_id MEDIUMINT NOT NULL DEFAULT -1,
                        main_chao_id MEDIUMINT NOT NULL DEFAULT -1,
                        sub_chao_id MEDIUMINT NOT NULL DEFAULT -1,

                        num_rings BIGINT UNSIGNED NOT NULL DEFAULT 0,
                        num_buy_rings BIGINT NOT NULL DEFAULT 0,
                        num_red_rings BIGINT UNSIGNED NOT NULL DEFAULT 0,
                        num_buy_red_rings BIGINT NOT NULL DEFAULT 0,
                        energy BIGINT NOT NULL DEFAULT 0,
                        energy_buy BIGINT NOT NULL DEFAULT 0,
                        energy_renews_at BIGINT NOT NULL DEFAULT 0,
                        num_messages BIGINT NOT NULL DEFAULT 0,
                        ranking_league BIGINT NOT NULL DEFAULT 0,
                        quick_ranking_league BIGINT NOT NULL DEFAULT 0,
                        num_roulette_ticket BIGINT NOT NULL DEFAULT 0,
                        num_chao_roulette_ticket BIGINT NOT NULL DEFAULT 0,
                        chao_eggs BIGINT NOT NULL DEFAULT 0,
                        story_high_score BIGINT UNSIGNED NOT NULL DEFAULT 0,
                        quick_high_score BIGINT UNSIGNED NOT NULL DEFAULT 0,
                        total_distance BIGINT UNSIGNED NOT NULL DEFAULT 0,
                        maximum_distance BIGINT NOT NULL DEFAULT 0,
                        daily_mission_id INTEGER NOT NULL DEFAULT 1,
                        daily_mission_end_time BIGINT NOT NULL DEFAULT 0,
                        daily_challenge_value INTEGER NOT NULL DEFAULT 0,
                        daily_challenge_complete BIGINT NOT NULL DEFAULT 0,
                        num_daily_challenge_cont BIGINT NOT NULL DEFAULT 0,
                        num_playing BIGINT NOT NULL DEFAULT 0,
                        num_animals BIGINT UNSIGNED NOT NULL DEFAULT 0,
                        num_rank INTEGER NOT NULL DEFAULT 0,
                        equip_item_list TINYTEXT NOT NULL DEFAULT ''
                    );
                    ALTER TABLE `sw_players` AUTO_INCREMENT=1000000000;
                    CREATE TABLE `sw_sessions` (
                        sid VARCHAR(48) NOT NULL PRIMARY KEY,
                        uid BIGINT UNSIGNED NOT NULL,
                        expiry BIGINT NOT NULL
                    );
                    CREATE TABLE `sw_itemownership` (
                        user_id BIGINT UNSIGNED NOT NULL,
                        item_id BIGINT UNSIGNED NOT NULL
                    );");
            }

            // Drop and recreate characters, characterstates, and characterupgrades
            if (characters)
            {
                QuickRun(conn,
                    @"DROP TABLE IF EXISTS `sw_characters`;
                    DROP TABLE IF EXISTS `sw_characterstates`;
                    DROP TABLE IF EXISTS `sw_characterupgrades`;
                    CREATE TABLE `sw_characters` (
                        id MEDIUMINT UNSIGNED NOT NULL PRIMARY KEY,
                        num_rings BIGINT UNSIGNED NOT NULL,
                        num_red_rings BIGINT UNSIGNED NOT NULL,
                        price_num_rings BIGINT UNSIGNED NOT NULL,
                        price_num_red_rings BIGINT UNSIGNED NOT NULL,
                        lock_condition TINYINT NOT NULL,
                        star_max INTEGER NOT NULL DEFAULT 10,
                        visible TINYINT NOT NULL
                    );
                    CREATE TABLE `sw_characterstates` (
                        user_id BIGINT UNSIGNED NOT NULL,
                        character_id BIGINT UNSIGNED NOT NULL,
                        status TINYINT NOT NULL,
                        level TINYINT NOT NULL,
                        exp BIGINT UNSIGNED NOT NULL,
                        star TINYINT NOT NULL,
                        ability_level TINYTEXT NOT NULL,
                        ability_num_rings TINYTEXT NOT NULL
                    );
                    INSERT INTO `sw_characters` (
                        id,
                        num_rings,
                        num_red_rings,
                        price_num_rings,
                        price_num_red_rings,
                        lock_condition,
                        star_max,
                        visible
                    ) VALUES (
                        '300000',
                        '200',
                        '0',
                        '0',
                        '0',
                        '0',
                        '10',
                        '1'
                    ),
                    (
                        '300001',
                        '200',
                        '0',
                        '0',
                        '0',
                        '1',
                        '10',
                        '0'
                    ),
                    (
                        '300002',
                        '200',
                        '0',
                        '0',
                        '0',
                        '1',
                        '10',
                        '0'
                    ),
                    (
                        '300003',
                        '300',
                        '0',
                        '2000000',
                        '200',
                        '2',
                        '10',
                        '1'
                    ),
                    (
                        '300004',
                        '300',
                        '0',
                        '2000000',
                        '200',
                        '2',
                        '10',
                        '1'
                    ),
                    (
                        '300005',
                        '300',
                        '0',
                        '0',
                        '0',
                        '3',
                        '10',
                        '1'
                    ),
                    (
                        '300006',
                        '300',
                        '0',
                        '2000000',
                        '200',
                        '2',
                        '10',
                        '1'
                    ),
                    (
                        '300007',
                        '300',
                        '0',
                        '2000000',
                        '200',
                        '2',
                        '10',
                        '1'
                    ),
                    (
                        '300008',
                        '40',
                        '0',
                        '1500000',
                        '150',
                        '2',
                        '10',
                        '1'
                    ),
                    (
                        '300009',
                        '40',
                        '0',
                        '1500000',
                        '150',
                        '2',
                        '10',
                        '1'
                    ),
                    (
                        '300010',
                        '300',
                        '0',
                        '2000000',
                        '200',
                        '2',
                        '10',
                        '1'
                    ),
                    (
                        '300011',
                        '300',
                        '0',
                        '2000000',
                        '200',
                        '2',
                        '10',
                        '1'
                    ),
                    (
                        '300012',
                        '300',
                        '0',
                        '2000000',
                        '200',
                        '2',
                        '10',
                        '1'
                    ),
                    (
                        '300013',
                        '40',
                        '0',
                        '1500000',
                        '150',
                        '2',
                        '10',
                        '1'
                    ),
                    (
                        '300014',
                        '300',
                        '0',
                        '0',
                        '0',
                        '3',
                        '10',
                        '1'
                    ),
                    (
                        '300015',
                        '300',
                        '0',
                        '0',
                        '0',
                        '3',
                        '10',
                        '1'
                    ),
                    (
                        '300016',
                        '300',
                        '0',
                        '0',
                        '0',
                        '3',
                        '10',
                        '1'
                    ),
                    (
                        '300017',
                        '300',
                        '0',
                        '0',
                        '0',
                        '3',
                        '10',
                        '1'
                    ),
                    (
                        '300018',
                        '300',
                        '0',
                        '0',
                        '0',
                        '3',
                        '10',
                        '1'
                    ),
                    (
                        '300019',
                        '300',
                        '0',
                        '0',
                        '0',
                        '3',
                        '10',
                        '1'
                    ),
                    (
                        '300020',
                        '300',
                        '0',
                        '0',
                        '0',
                        '3',
                        '10',
                        '1'
                    ),
                    (
                        '301000',
                        '300',
                        '0',
                        '0',
                        '0',
                        '3',
                        '10',
                        '0'
                    ),
                    (
                        '301001',
                        '300',
                        '0',
                        '0',
                        '0',
                        '3',
                        '10',
                        '0'
                    ),
                    (
                        '301002',
                        '300',
                        '0',
                        '0',
                        '0',
                        '3',
                        '10',
                        '0'
                    ),
                    (
                        '301003',
                        '300',
                        '0',
                        '0',
                        '0',
                        '3',
                        '10',
                        '0'
                    ),
                    (
                        '301004',
                        '300',
                        '0',
                        '0',
                        '0',
                        '3',
                        '10',
                        '0'
                    ),
                    (
                        '301005',
                        '300',
                        '0',
                        '0',
                        '0',
                        '3',
                        '10',
                        '0'
                    ),
                    (
                        '301006',
                        '300',
                        '0',
                        '0',
                        '0',
                        '3',
                        '10',
                        '0'
                    ),
                    (
                        '301007',
                        '300',
                        '0',
                        '0',
                        '0',
                        '3',
                        '10',
                        '0'
                    );
                    CREATE TABLE `sw_characterupgrades` (
                        character_id MEDIUMINT UNSIGNED NOT NULL,
                        min_level TINYINT NOT NULL,
                        max_level TINYINT NOT NULL,
                        multiple MEDIUMINT NOT NULL
                    );
                    INSERT INTO `sw_characterupgrades`
                    VALUES (
                        '300000',
                        '0',
                        '9',
                        '200'
                    ),
                    (
                        '300000',
                        '10',
                        '19',
                        '400'
                    ),
                    (
                        '300000',
                        '20',
                        '29',
                        '600'
                    ),
                    (
                        '300000',
                        '30',
                        '39',
                        '800'
                    ),
                    (
                        '300000',
                        '40',
                        '49',
                        '1000'
                    ),
                    (
                        '300000',
                        '50',
                        '59',
                        '1200'
                    ),
                    (
                        '300000',
                        '60',
                        '69',
                        '1400'
                    ),
                    (
                        '300000',
                        '70',
                        '79',
                        '1600'
                    ),
                    (
                        '300000',
                        '80',
                        '89',
                        '1800'
                    ),
                    (
                        '300000',
                        '90',
                        '100',
                        '2000'
                    ),
                    (
                        '300001',
                        '0',
                        '9',
                        '200'
                    ),
                    (
                        '300001',
                        '10',
                        '19',
                        '400'
                    ),
                    (
                        '300001',
                        '20',
                        '29',
                        '600'
                    ),
                    (
                        '300001',
                        '30',
                        '39',
                        '800'
                    ),
                    (
                        '300001',
                        '40',
                        '49',
                        '1000'
                    ),
                    (
                        '300001',
                        '50',
                        '59',
                        '1200'
                    ),
                    (
                        '300001',
                        '60',
                        '69',
                        '1400'
                    ),
                    (
                        '300001',
                        '70',
                        '79',
                        '1600'
                    ),
                    (
                        '300001',
                        '80',
                        '89',
                        '1800'
                    ),
                    (
                        '300001',
                        '90',
                        '100',
                        '2000'
                    ),
                    (
                        '300002',
                        '0',
                        '9',
                        '200'
                    ),
                    (
                        '300002',
                        '10',
                        '19',
                        '400'
                    ),
                    (
                        '300002',
                        '20',
                        '29',
                        '600'
                    ),
                    (
                        '300002',
                        '30',
                        '39',
                        '800'
                    ),
                    (
                        '300002',
                        '40',
                        '49',
                        '1000'
                    ),
                    (
                        '300002',
                        '50',
                        '59',
                        '1200'
                    ),
                    (
                        '300002',
                        '60',
                        '69',
                        '1400'
                    ),
                    (
                        '300002',
                        '70',
                        '79',
                        '1600'
                    ),
                    (
                        '300002',
                        '80',
                        '89',
                        '1800'
                    ),
                    (
                        '300002',
                        '90',
                        '100',
                        '2000'
                    ),
                    (
                        '300003',
                        '0',
                        '9',
                        '300'
                    ),
                    (
                        '300003',
                        '10',
                        '19',
                        '600'
                    ),
                    (
                        '300003',
                        '20',
                        '29',
                        '900'
                    ),
                    (
                        '300003',
                        '30',
                        '39',
                        '1200'
                    ),
                    (
                        '300003',
                        '40',
                        '49',
                        '1500'
                    ),
                    (
                        '300003',
                        '50',
                        '59',
                        '1800'
                    ),
                    (
                        '300003',
                        '60',
                        '69',
                        '2100'
                    ),
                    (
                        '300003',
                        '70',
                        '79',
                        '2400'
                    ),
                    (
                        '300003',
                        '80',
                        '89',
                        '2700'
                    ),
                    (
                        '300003',
                        '90',
                        '100',
                        '3000'
                    ),
                    (
                        '300004',
                        '0',
                        '9',
                        '300'
                    ),
                    (
                        '300004',
                        '10',
                        '19',
                        '600'
                    ),
                    (
                        '300004',
                        '20',
                        '29',
                        '900'
                    ),
                    (
                        '300004',
                        '30',
                        '39',
                        '1200'
                    ),
                    (
                        '300004',
                        '40',
                        '49',
                        '1500'
                    ),
                    (
                        '300004',
                        '50',
                        '59',
                        '1800'
                    ),
                    (
                        '300004',
                        '60',
                        '69',
                        '2100'
                    ),
                    (
                        '300004',
                        '70',
                        '79',
                        '2400'
                    ),
                    (
                        '300004',
                        '80',
                        '89',
                        '2700'
                    ),
                    (
                        '300004',
                        '90',
                        '100',
                        '3000'
                    ),
                    (
                        '300005',
                        '0',
                        '9',
                        '300'
                    ),
                    (
                        '300005',
                        '10',
                        '19',
                        '600'
                    ),
                    (
                        '300005',
                        '20',
                        '29',
                        '900'
                    ),
                    (
                        '300005',
                        '30',
                        '39',
                        '1200'
                    ),
                    (
                        '300005',
                        '40',
                        '49',
                        '1500'
                    ),
                    (
                        '300005',
                        '50',
                        '59',
                        '1800'
                    ),
                    (
                        '300005',
                        '60',
                        '69',
                        '2100'
                    ),
                    (
                        '300005',
                        '70',
                        '79',
                        '2400'
                    ),
                    (
                        '300005',
                        '80',
                        '89',
                        '2700'
                    ),
                    (
                        '300005',
                        '90',
                        '100',
                        '3000'
                    ),
                    (
                        '300006',
                        '0',
                        '9',
                        '300'
                    ),
                    (
                        '300006',
                        '10',
                        '19',
                        '600'
                    ),
                    (
                        '300006',
                        '20',
                        '29',
                        '900'
                    ),
                    (
                        '300006',
                        '30',
                        '39',
                        '1200'
                    ),
                    (
                        '300006',
                        '40',
                        '49',
                        '1500'
                    ),
                    (
                        '300006',
                        '50',
                        '59',
                        '1800'
                    ),
                    (
                        '300006',
                        '60',
                        '69',
                        '2100'
                    ),
                    (
                        '300006',
                        '70',
                        '79',
                        '2400'
                    ),
                    (
                        '300006',
                        '80',
                        '89',
                        '2700'
                    ),
                    (
                        '300006',
                        '90',
                        '100',
                        '3000'
                    ),
                    (
                        '300007',
                        '0',
                        '9',
                        '300'
                    ),
                    (
                        '300007',
                        '10',
                        '19',
                        '600'
                    ),
                    (
                        '300007',
                        '20',
                        '29',
                        '900'
                    ),
                    (
                        '300007',
                        '30',
                        '39',
                        '1200'
                    ),
                    (
                        '300007',
                        '40',
                        '49',
                        '1500'
                    ),
                    (
                        '300007',
                        '50',
                        '59',
                        '1800'
                    ),
                    (
                        '300007',
                        '60',
                        '69',
                        '2100'
                    ),
                    (
                        '300007',
                        '70',
                        '79',
                        '2400'
                    ),
                    (
                        '300007',
                        '80',
                        '89',
                        '2700'
                    ),
                    (
                        '300007',
                        '90',
                        '100',
                        '3000'
                    ),
                    (
                        '300008',
                        '0',
                        '9',
                        '40'
                    ),
                    (
                        '300008',
                        '10',
                        '19',
                        '80'
                    ),
                    (
                        '300008',
                        '20',
                        '29',
                        '120'
                    ),
                    (
                        '300008',
                        '30',
                        '39',
                        '160'
                    ),
                    (
                        '300008',
                        '40',
                        '49',
                        '200'
                    ),
                    (
                        '300008',
                        '50',
                        '59',
                        '240'
                    ),
                    (
                        '300008',
                        '60',
                        '69',
                        '280'
                    ),
                    (
                        '300008',
                        '70',
                        '79',
                        '320'
                    ),
                    (
                        '300008',
                        '80',
                        '89',
                        '360'
                    ),
                    (
                        '300008',
                        '90',
                        '100',
                        '400'
                    ),
                    (
                        '300009',
                        '0',
                        '9',
                        '40'
                    ),
                    (
                        '300009',
                        '10',
                        '19',
                        '80'
                    ),
                    (
                        '300009',
                        '20',
                        '29',
                        '120'
                    ),
                    (
                        '300009',
                        '30',
                        '39',
                        '160'
                    ),
                    (
                        '300009',
                        '40',
                        '49',
                        '200'
                    ),
                    (
                        '300009',
                        '50',
                        '59',
                        '240'
                    ),
                    (
                        '300009',
                        '60',
                        '69',
                        '280'
                    ),
                    (
                        '300009',
                        '70',
                        '79',
                        '320'
                    ),
                    (
                        '300009',
                        '80',
                        '89',
                        '360'
                    ),
                    (
                        '300009',
                        '90',
                        '100',
                        '400'
                    ),
                    (
                        '300010',
                        '0',
                        '9',
                        '300'
                    ),
                    (
                        '300010',
                        '10',
                        '19',
                        '600'
                    ),
                    (
                        '300010',
                        '20',
                        '29',
                        '900'
                    ),
                    (
                        '300010',
                        '30',
                        '39',
                        '1200'
                    ),
                    (
                        '300010',
                        '40',
                        '49',
                        '1500'
                    ),
                    (
                        '300010',
                        '50',
                        '59',
                        '1800'
                    ),
                    (
                        '300010',
                        '60',
                        '69',
                        '2100'
                    ),
                    (
                        '300010',
                        '70',
                        '79',
                        '2400'
                    ),
                    (
                        '300010',
                        '80',
                        '89',
                        '2700'
                    ),
                    (
                        '300010',
                        '90',
                        '100',
                        '3000'
                    ),
                    (
                        '300011',
                        '0',
                        '9',
                        '300'
                    ),
                    (
                        '300011',
                        '10',
                        '19',
                        '600'
                    ),
                    (
                        '300011',
                        '20',
                        '29',
                        '900'
                    ),
                    (
                        '300011',
                        '30',
                        '39',
                        '1200'
                    ),
                    (
                        '300011',
                        '40',
                        '49',
                        '1500'
                    ),
                    (
                        '300011',
                        '50',
                        '59',
                        '1800'
                    ),
                    (
                        '300011',
                        '60',
                        '69',
                        '2100'
                    ),
                    (
                        '300011',
                        '70',
                        '79',
                        '2400'
                    ),
                    (
                        '300011',
                        '80',
                        '89',
                        '2700'
                    ),
                    (
                        '300011',
                        '90',
                        '100',
                        '3000'
                    ),
                    (
                        '300012',
                        '0',
                        '9',
                        '300'
                    ),
                    (
                        '300012',
                        '10',
                        '19',
                        '600'
                    ),
                    (
                        '300012',
                        '20',
                        '29',
                        '900'
                    ),
                    (
                        '300012',
                        '30',
                        '39',
                        '1200'
                    ),
                    (
                        '300012',
                        '40',
                        '49',
                        '1500'
                    ),
                    (
                        '300012',
                        '50',
                        '59',
                        '1800'
                    ),
                    (
                        '300012',
                        '60',
                        '69',
                        '2100'
                    ),
                    (
                        '300012',
                        '70',
                        '79',
                        '2400'
                    ),
                    (
                        '300012',
                        '80',
                        '89',
                        '2700'
                    ),
                    (
                        '300012',
                        '90',
                        '100',
                        '3000'
                    ),
                    (
                        '300013',
                        '0',
                        '9',
                        '40'
                    ),
                    (
                        '300013',
                        '10',
                        '19',
                        '80'
                    ),
                    (
                        '300013',
                        '20',
                        '29',
                        '120'
                    ),
                    (
                        '300013',
                        '30',
                        '39',
                        '160'
                    ),
                    (
                        '300013',
                        '40',
                        '49',
                        '200'
                    ),
                    (
                        '300013',
                        '50',
                        '59',
                        '240'
                    ),
                    (
                        '300013',
                        '60',
                        '69',
                        '280'
                    ),
                    (
                        '300013',
                        '70',
                        '79',
                        '320'
                    ),
                    (
                        '300013',
                        '80',
                        '89',
                        '360'
                    ),
                    (
                        '300013',
                        '90',
                        '100',
                        '400'
                    ),
                    (
                        '300014',
                        '0',
                        '9',
                        '300'
                    ),
                    (
                        '300014',
                        '10',
                        '19',
                        '600'
                    ),
                    (
                        '300014',
                        '20',
                        '29',
                        '900'
                    ),
                    (
                        '300014',
                        '30',
                        '39',
                        '1200'
                    ),
                    (
                        '300014',
                        '40',
                        '49',
                        '1500'
                    ),
                    (
                        '300014',
                        '50',
                        '59',
                        '1800'
                    ),
                    (
                        '300014',
                        '60',
                        '69',
                        '2100'
                    ),
                    (
                        '300014',
                        '70',
                        '79',
                        '2400'
                    ),
                    (
                        '300014',
                        '80',
                        '89',
                        '2700'
                    ),
                    (
                        '300014',
                        '90',
                        '100',
                        '3000'
                    ),
                    (
                        '300015',
                        '0',
                        '9',
                        '300'
                    ),
                    (
                        '300015',
                        '10',
                        '19',
                        '600'
                    ),
                    (
                        '300015',
                        '20',
                        '29',
                        '900'
                    ),
                    (
                        '300015',
                        '30',
                        '39',
                        '1200'
                    ),
                    (
                        '300015',
                        '40',
                        '49',
                        '1500'
                    ),
                    (
                        '300015',
                        '50',
                        '59',
                        '1800'
                    ),
                    (
                        '300015',
                        '60',
                        '69',
                        '2100'
                    ),
                    (
                        '300015',
                        '70',
                        '79',
                        '2400'
                    ),
                    (
                        '300015',
                        '80',
                        '89',
                        '2700'
                    ),
                    (
                        '300015',
                        '90',
                        '100',
                        '3000'
                    ),
                    (
                        '300016',
                        '0',
                        '9',
                        '300'
                    ),
                    (
                        '300016',
                        '10',
                        '19',
                        '600'
                    ),
                    (
                        '300016',
                        '20',
                        '29',
                        '900'
                    ),
                    (
                        '300016',
                        '30',
                        '39',
                        '1200'
                    ),
                    (
                        '300016',
                        '40',
                        '49',
                        '1500'
                    ),
                    (
                        '300016',
                        '50',
                        '59',
                        '1800'
                    ),
                    (
                        '300016',
                        '60',
                        '69',
                        '2100'
                    ),
                    (
                        '300016',
                        '70',
                        '79',
                        '2400'
                    ),
                    (
                        '300016',
                        '80',
                        '89',
                        '2700'
                    ),
                    (
                        '300016',
                        '90',
                        '100',
                        '3000'
                    ),
                    (
                        '300017',
                        '0',
                        '9',
                        '300'
                    ),
                    (
                        '300017',
                        '10',
                        '19',
                        '600'
                    ),
                    (
                        '300017',
                        '20',
                        '29',
                        '900'
                    ),
                    (
                        '300017',
                        '30',
                        '39',
                        '1200'
                    ),
                    (
                        '300017',
                        '40',
                        '49',
                        '1500'
                    ),
                    (
                        '300017',
                        '50',
                        '59',
                        '1800'
                    ),
                    (
                        '300017',
                        '60',
                        '69',
                        '2100'
                    ),
                    (
                        '300017',
                        '70',
                        '79',
                        '2400'
                    ),
                    (
                        '300017',
                        '80',
                        '89',
                        '2700'
                    ),
                    (
                        '300017',
                        '90',
                        '100',
                        '3000'
                    ),
                    (
                        '300018',
                        '0',
                        '9',
                        '300'
                    ),
                    (
                        '300018',
                        '10',
                        '19',
                        '600'
                    ),
                    (
                        '300018',
                        '20',
                        '29',
                        '900'
                    ),
                    (
                        '300018',
                        '30',
                        '39',
                        '1200'
                    ),
                    (
                        '300018',
                        '40',
                        '49',
                        '1500'
                    ),
                    (
                        '300018',
                        '50',
                        '59',
                        '1800'
                    ),
                    (
                        '300018',
                        '60',
                        '69',
                        '2100'
                    ),
                    (
                        '300018',
                        '70',
                        '79',
                        '2400'
                    ),
                    (
                        '300018',
                        '80',
                        '89',
                        '2700'
                    ),
                    (
                        '300018',
                        '90',
                        '100',
                        '3000'
                    ),
                    (
                        '300019',
                        '0',
                        '9',
                        '300'
                    ),
                    (
                        '300019',
                        '10',
                        '19',
                        '600'
                    ),
                    (
                        '300019',
                        '20',
                        '29',
                        '900'
                    ),
                    (
                        '300019',
                        '30',
                        '39',
                        '1200'
                    ),
                    (
                        '300019',
                        '40',
                        '49',
                        '1500'
                    ),
                    (
                        '300019',
                        '50',
                        '59',
                        '1800'
                    ),
                    (
                        '300019',
                        '60',
                        '69',
                        '2100'
                    ),
                    (
                        '300019',
                        '70',
                        '79',
                        '2400'
                    ),
                    (
                        '300019',
                        '80',
                        '89',
                        '2700'
                    ),
                    (
                        '300019',
                        '90',
                        '100',
                        '3000'
                    ),
                    (
                        '300020',
                        '0',
                        '9',
                        '300'
                    ),
                    (
                        '300020',
                        '10',
                        '19',
                        '600'
                    ),
                    (
                        '300020',
                        '20',
                        '29',
                        '900'
                    ),
                    (
                        '300020',
                        '30',
                        '39',
                        '1200'
                    ),
                    (
                        '300020',
                        '40',
                        '49',
                        '1500'
                    ),
                    (
                        '300020',
                        '50',
                        '59',
                        '1800'
                    ),
                    (
                        '300020',
                        '60',
                        '69',
                        '2100'
                    ),
                    (
                        '300020',
                        '70',
                        '79',
                        '2400'
                    ),
                    (
                        '300020',
                        '80',
                        '89',
                        '2700'
                    ),
                    (
                        '300020',
                        '90',
                        '100',
                        '3000'
                    ),
                    (
                        '301000',
                        '0',
                        '9',
                        '300'
                    ),
                    (
                        '301000',
                        '10',
                        '19',
                        '600'
                    ),
                    (
                        '301000',
                        '20',
                        '29',
                        '900'
                    ),
                    (
                        '301000',
                        '30',
                        '39',
                        '1200'
                    ),
                    (
                        '301000',
                        '40',
                        '49',
                        '1500'
                    ),
                    (
                        '301000',
                        '50',
                        '59',
                        '1800'
                    ),
                    (
                        '301000',
                        '60',
                        '69',
                        '2100'
                    ),
                    (
                        '301000',
                        '70',
                        '79',
                        '2400'
                    ),
                    (
                        '301000',
                        '80',
                        '89',
                        '2700'
                    ),
                    (
                        '301000',
                        '90',
                        '100',
                        '3000'
                    ),
                    (
                        '301001',
                        '0',
                        '9',
                        '300'
                    ),
                    (
                        '301001',
                        '10',
                        '19',
                        '600'
                    ),
                    (
                        '301001',
                        '20',
                        '29',
                        '900'
                    ),
                    (
                        '301001',
                        '30',
                        '39',
                        '1200'
                    ),
                    (
                        '301001',
                        '40',
                        '49',
                        '1500'
                    ),
                    (
                        '301001',
                        '50',
                        '59',
                        '1800'
                    ),
                    (
                        '301001',
                        '60',
                        '69',
                        '2100'
                    ),
                    (
                        '301001',
                        '70',
                        '79',
                        '2400'
                    ),
                    (
                        '301001',
                        '80',
                        '89',
                        '2700'
                    ),
                    (
                        '301001',
                        '90',
                        '100',
                        '3000'
                    ),
                    (
                        '301002',
                        '0',
                        '9',
                        '300'
                    ),
                    (
                        '301002',
                        '10',
                        '19',
                        '600'
                    ),
                    (
                        '301002',
                        '20',
                        '29',
                        '900'
                    ),
                    (
                        '301002',
                        '30',
                        '39',
                        '1200'
                    ),
                    (
                        '301002',
                        '40',
                        '49',
                        '1500'
                    ),
                    (
                        '301002',
                        '50',
                        '59',
                        '1800'
                    ),
                    (
                        '301002',
                        '60',
                        '69',
                        '2100'
                    ),
                    (
                        '301002',
                        '70',
                        '79',
                        '2400'
                    ),
                    (
                        '301002',
                        '80',
                        '89',
                        '2700'
                    ),
                    (
                        '301002',
                        '90',
                        '100',
                        '3000'
                    ),
                    (
                        '301003',
                        '0',
                        '9',
                        '300'
                    ),
                    (
                        '301003',
                        '10',
                        '19',
                        '600'
                    ),
                    (
                        '301003',
                        '20',
                        '29',
                        '900'
                    ),
                    (
                        '301003',
                        '30',
                        '39',
                        '1200'
                    ),
                    (
                        '301003',
                        '40',
                        '49',
                        '1500'
                    ),
                    (
                        '301003',
                        '50',
                        '59',
                        '1800'
                    ),
                    (
                        '301003',
                        '60',
                        '69',
                        '2100'
                    ),
                    (
                        '301003',
                        '70',
                        '79',
                        '2400'
                    ),
                    (
                        '301003',
                        '80',
                        '89',
                        '2700'
                    ),
                    (
                        '301003',
                        '90',
                        '100',
                        '3000'
                    ),
                    (
                        '301004',
                        '0',
                        '9',
                        '300'
                    ),
                    (
                        '301004',
                        '10',
                        '19',
                        '600'
                    ),
                    (
                        '301004',
                        '20',
                        '29',
                        '900'
                    ),
                    (
                        '301004',
                        '30',
                        '39',
                        '1200'
                    ),
                    (
                        '301004',
                        '40',
                        '49',
                        '1500'
                    ),
                    (
                        '301004',
                        '50',
                        '59',
                        '1800'
                    ),
                    (
                        '301004',
                        '60',
                        '69',
                        '2100'
                    ),
                    (
                        '301004',
                        '70',
                        '79',
                        '2400'
                    ),
                    (
                        '301004',
                        '80',
                        '89',
                        '2700'
                    ),
                    (
                        '301004',
                        '90',
                        '100',
                        '3000'
                    ),
                    (
                        '301005',
                        '0',
                        '9',
                        '300'
                    ),
                    (
                        '301005',
                        '10',
                        '19',
                        '600'
                    ),
                    (
                        '301005',
                        '20',
                        '29',
                        '900'
                    ),
                    (
                        '301005',
                        '30',
                        '39',
                        '1200'
                    ),
                    (
                        '301005',
                        '40',
                        '49',
                        '1500'
                    ),
                    (
                        '301005',
                        '50',
                        '59',
                        '1800'
                    ),
                    (
                        '301005',
                        '60',
                        '69',
                        '2100'
                    ),
                    (
                        '301005',
                        '70',
                        '79',
                        '2400'
                    ),
                    (
                        '301005',
                        '80',
                        '89',
                        '2700'
                    ),
                    (
                        '301005',
                        '90',
                        '100',
                        '3000'
                    ),
                    (
                        '301006',
                        '0',
                        '9',
                        '300'
                    ),
                    (
                        '301006',
                        '10',
                        '19',
                        '600'
                    ),
                    (
                        '301006',
                        '20',
                        '29',
                        '900'
                    ),
                    (
                        '301006',
                        '30',
                        '39',
                        '1200'
                    ),
                    (
                        '301006',
                        '40',
                        '49',
                        '1500'
                    ),
                    (
                        '301006',
                        '50',
                        '59',
                        '1800'
                    ),
                    (
                        '301006',
                        '60',
                        '69',
                        '2100'
                    ),
                    (
                        '301006',
                        '70',
                        '79',
                        '2400'
                    ),
                    (
                        '301006',
                        '80',
                        '89',
                        '2700'
                    ),
                    (
                        '301006',
                        '90',
                        '100',
                        '3000'
                    ),
                    (
                        '301007',
                        '0',
                        '9',
                        '300'
                    ),
                    (
                        '301007',
                        '10',
                        '19',
                        '600'
                    ),
                    (
                        '301007',
                        '20',
                        '29',
                        '900'
                    ),
                    (
                        '301007',
                        '30',
                        '39',
                        '1200'
                    ),
                    (
                        '301007',
                        '40',
                        '49',
                        '1500'
                    ),
                    (
                        '301007',
                        '50',
                        '59',
                        '1800'
                    ),
                    (
                        '301007',
                        '60',
                        '69',
                        '2100'
                    ),
                    (
                        '301007',
                        '70',
                        '79',
                        '2400'
                    ),
                    (
                        '301007',
                        '80',
                        '89',
                        '2700'
                    ),
                    (
                        '301007',
                        '90',
                        '100',
                        '3000'
                    );");
            }

            // Drop and recreate mileagemapstates
            if (mileageMapStates)
            {
                QuickRun(conn,
                    @"DROP TABLE IF EXISTS `sw_mileagemapstates`;
                    CREATE TABLE `sw_mileagemapstates` (
                        user_id BIGINT UNSIGNED NOT NULL,
                        episode TINYINT NOT NULL,
                        chapter TINYINT NOT NULL,
                        point BIGINT NOT NULL,
                        stage_total_score BIGINT UNSIGNED NOT NULL,
                        chapter_start_time BIGINT NOT NULL,

                        map_distance BIGINT NOT NULL,
                        num_boss_attack BIGINT NOT NULL,
                        stage_distance BIGINT NOT NULL,
                        stage_max_score BIGINT UNSIGNED NOT NULL
                    );");
            }

            if (config)
            {
                QuickRun(conn,
                    @"DROP TABLE IF EXISTS `sw_config`;
                    CREATE TABLE `sw_config` (
                        id TINYINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
                        is_maintenance TINYINT NOT NULL DEFAULT 0,
                        support_legacy_versions TINYINT NOT NULL DEFAULT 1,
                        debug_log TINYINT NOT NULL DEFAULT 0,
                        enable_debug_endpoints TINYINT NOT NULL DEFAULT 0,
                        encryption_iv VARCHAR(16) NOT NULL DEFAULT 'burgersMetKortin',
                        session_time INT NOT NULL DEFAULT 3600,
                        assets_version VARCHAR(3) NOT NULL DEFAULT '049',
                        client_version VARCHAR(8) NOT NULL DEFAULT '2.0.3',
                        data_version VARCHAR(2) NOT NULL DEFAULT '15',
                        info_version VARCHAR(3) NOT NULL DEFAULT '017',
                        revive_rsr_cost BIGINT UNSIGNED NOT NULL DEFAULT 5,
                        enable_limited_time_incentives TINYINT NOT NULL DEFAULT 1
                    );
                    INSERT INTO `sw_config` (id) VALUES ('1');");
            }

            if (tickers)
            {
                QuickRun(conn,
                    @"DROP TABLE IF EXISTS `sw_tickers`;
                    CREATE TABLE `sw_tickers` (
                        id TINYINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
                        start_time BIGINT UNSIGNED NOT NULL,
                        end_time BIGINT UNSIGNED NOT NULL,
                        message VARCHAR(600) NOT NULL,
                        language TINYINT NOT NULL
                    );");
            }

            if (dailyChallenge)
            {
                QuickRun(conn,
                    @"DROP TABLE IF EXISTS `sw_dailychallenge`;
                    CREATE TABLE `sw_dailychallenge` (
                        id TINYINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
                        item1 MEDIUMINT NOT NULL DEFAULT 910000,
                        item1_count BIGINT UNSIGNED NOT NULL DEFAULT 1000,
                        item2 MEDIUMINT NOT NULL DEFAULT 900000,
                        item2_count BIGINT UNSIGNED NOT NULL DEFAULT 10,
                        item3 MEDIUMINT NOT NULL DEFAULT 910000,
                        item3_count BIGINT UNSIGNED NOT NULL DEFAULT 5000,
                        item4 MEDIUMINT NOT NULL DEFAULT 900000,
                        item4_count BIGINT UNSIGNED NOT NULL DEFAULT 20,
                        item5 MEDIUMINT NOT NULL DEFAULT 910000,
                        item5_count BIGINT UNSIGNED NOT NULL DEFAULT 10000,
                        item6 MEDIUMINT NOT NULL DEFAULT 900000,
                        item6_count BIGINT UNSIGNED NOT NULL DEFAULT 30,
                        item7 MEDIUMINT NOT NULL DEFAULT 900000,
                        item7_count BIGINT UNSIGNED NOT NULL DEFAULT 60
                    );
                    INSERT INTO `sw_dailychallenge` (id) VALUES ('1');");
            }

            if (costs)
            {
                QuickRun(conn,
                    @"DROP TABLE IF EXISTS `sw_costlist`;
                    CREATE TABLE `sw_costlist` (
                        id TINYINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
                        item1 MEDIUMINT NOT NULL DEFAULT 910000,
                        item1_cost BIGINT UNSIGNED NOT NULL DEFAULT 6000,
                        item1_id MEDIUMINT NOT NULL DEFAULT 110000,
                        item2 MEDIUMINT NOT NULL DEFAULT 910000,
                        item2_cost BIGINT UNSIGNED NOT NULL DEFAULT 1000,
                        item2_id MEDIUMINT NOT NULL DEFAULT 110001,
                        item3 MEDIUMINT NOT NULL DEFAULT 910000,
                        item3_cost BIGINT UNSIGNED NOT NULL DEFAULT 4000,
                        item3_id MEDIUMINT NOT NULL DEFAULT 110002,

                        item4 MEDIUMINT NOT NULL DEFAULT 910000,
                        item4_cost BIGINT UNSIGNED NOT NULL DEFAULT 3000,
                        item4_id MEDIUMINT NOT NULL DEFAULT 120000,
                        item5 MEDIUMINT NOT NULL DEFAULT 910000,
                        item5_cost BIGINT UNSIGNED NOT NULL DEFAULT 1000,
                        item5_id MEDIUMINT NOT NULL DEFAULT 120001,
                        item6 MEDIUMINT NOT NULL DEFAULT 910000,
                        item6_cost BIGINT UNSIGNED NOT NULL DEFAULT 3000,
                        item6_id MEDIUMINT NOT NULL DEFAULT 120002,
                        item7 MEDIUMINT NOT NULL DEFAULT 910000,
                        item7_cost BIGINT UNSIGNED NOT NULL DEFAULT 2000,
                        item7_id MEDIUMINT NOT NULL DEFAULT 120003,
                        item8 MEDIUMINT NOT NULL DEFAULT 910000,
                        item8_cost BIGINT UNSIGNED NOT NULL DEFAULT 3000,
                        item8_id MEDIUMINT NOT NULL DEFAULT 120004,
                        item9 MEDIUMINT NOT NULL DEFAULT 910000,
                        item9_cost BIGINT UNSIGNED NOT NULL DEFAULT 5000,
                        item9_id MEDIUMINT NOT NULL DEFAULT 120005,
                        item10 MEDIUMINT NOT NULL DEFAULT 910000,
                        item10_cost BIGINT UNSIGNED NOT NULL DEFAULT 4000,
                        item10_id MEDIUMINT NOT NULL DEFAULT 120006,
                        item11 MEDIUMINT NOT NULL DEFAULT 910000,
                        item11_cost BIGINT UNSIGNED NOT NULL DEFAULT 5000,
                        item11_id MEDIUMINT NOT NULL DEFAULT 120007,

                        item12 MEDIUMINT NOT NULL DEFAULT 900000,
                        item12_cost BIGINT UNSIGNED NOT NULL DEFAULT 5,
                        item12_id MEDIUMINT NOT NULL DEFAULT 950000,
                        item13 MEDIUMINT NOT NULL DEFAULT 900000,
                        item13_cost BIGINT UNSIGNED NOT NULL DEFAULT 2,
                        item13_id MEDIUMINT NOT NULL DEFAULT 980000,
                        item14 MEDIUMINT NOT NULL DEFAULT 900000,
                        item14_cost BIGINT UNSIGNED NOT NULL DEFAULT 5,
                        item14_id MEDIUMINT NOT NULL DEFAULT 980001,
                        item15 MEDIUMINT NOT NULL DEFAULT 900000,
                        item15_cost BIGINT UNSIGNED NOT NULL DEFAULT 10,
                        item15_id MEDIUMINT NOT NULL DEFAULT 980002
                    );
                    INSERT INTO `sw_costlist` (id) VALUES ('1');");
            }

            if (information)
            {
                QuickRun(conn,
                    @"DROP TABLE IF EXISTS `sw_information`;
                    CREATE TABLE `sw_information` (
                        id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                        priority TINYINT NOT NULL,
                        info_type TINYINT NOT NULL,
                        display_type TINYINT NOT NULL,
                        start_time BIGINT UNSIGNED NOT NULL,
                        end_time BIGINT UNSIGNED NOT NULL,
                        message VARCHAR(1000) NOT NULL,
                        image_id TINYTEXT NOT NULL,
                        extra MEDIUMTEXT NOT NULL,
                        language TINYINT NOT NULL
                    );");
            }

            conn.Close();
        }

        private static string m_dbHost = "";
        private static string m_dbUser = "";
        private static string m_dbPass = "";
        private static short m_dbPort = 0;
        private static string m_dbName = "";
    }
}
