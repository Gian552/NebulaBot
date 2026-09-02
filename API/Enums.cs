using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static NebulaBot.API.Roles;

namespace NebulaBot.API
{
    public static class Roles
    {
        public enum DiscordRoles : ulong
        {
            // misc
            [Description("Misc")]
            None = 0,
            [Description("Misc")]
            Verified = 1418676310888288396, 

            // Team
            [Description("Team")]
            admin = 1357113653513556138,
            [Description("Team")]
            ek = 1378381250317779066,
            [Description("Team")]
            jr_ek = 1378381487665315890,
            [Description("Team")]
            jr_admin = 1376365031503171636,
            [Description("Team")]
            teamleitung = 1365994618201706558,
            [Description("Team")]
            jr_teamleitung = 1416502573707432069,
            [Description("Team")]
            jr_devleitung = 1410678045240463410,
            [Description("Team")]
            devleitung = 1358498999589666822,
            [Description("Team")]
            moderator = 1358500228457693245,
            [Description("Team")]
            jr_mod = 1410610388571000943,
            [Description("Team")]
            supporter = 1358500836354949313,
            [Description("Team")]
            jr_supporter = 1358500946799362138,

            // Playtime
            [Description("Playtime")]
            explained = 1359898719176097852,
            [Description("Playtime")]
            archon = 1359897548382273697,
            [Description("Playtime")]
            neutralized = 1359897236460277790,
            [Description("Playtime")]
            appolyon = 1359897385161199696,
            [Description("Playtime")]
            thaumiel = 1359895917754319168,
            [Description("Playtime")]
            keter = 1359895815832993943,
            [Description("Playtime")]
            euclid = 1359895763458719765,
            [Description("Playtime")]
            safe = 1359894271896850653,
            [Description("Playtime")]
            pending = 1359897053265657916,

            // Cosmetic
            [Description("Cosmetic")]
            femboy = 1397315198058234029,
            [Description("Cosmetic")]
            lgbtq = 1397315521887862824,
            [Description("Cosmetic")]
            furry = 1397315446012641451,
            [Description("Cosmetic")]
            booster = 1361871816494416093,
            [Description("Cosmetic")]
            xi_8 = 1378720091872694352,

            // Rewards
            [Description("Rewards")]
            iota_10 = 1378720510153850920,
            [Description("Rewards")]
            mu_3 = 1365749086665445537,
            [Description("Rewards")]
            epsilon_6 = 1365749046215577651,
            [Description("Rewards")]
            psi_7 = 1365746129052106762,
            [Description("Rewards")]
            tau_5 = 1396819907324285090,
            [Description("Rewards")]
            nu_7 = 1396820040455684106,
            [Description("Rewards")]
            zeta_9 = 1396820219913048074,
            [Description("Rewards")]
            beta_7 = 1396820645240770580,
            [Description("Rewards")]
            beta_1 = 1396820854565765230,
            [Description("Rewards")]
            epsilon_11 = 1396820981158510633,
            [Description("Rewards")]
            alpha_1 = 1396821234163126302,
            [Description("Rewards")]
            omega_1 = 1396821351314231377,
            [Description("Rewards")]
            mu_4 = 1396821976554930267,
            [Description("Rewards")]
            eta_10 = 1396822165734690917,
        }

        public static string GetDiscordRoleType(this DiscordRoles value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? value.ToString();
        }

        internal static bool IsTeamler(this Database.API.PlayerData ply)
        {
            var dcRoles = ply.dcRoles;

            foreach (DiscordRoles role in dcRoles)
            {
                if (role.GetDiscordRoleType() == "Team")
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool IsTeamler(this IReadOnlyCollection<ulong> roles)
        {
            foreach (ulong roleId in roles)
            {
                DiscordRoles role = GetDiscordRoleById(roleId);
                if (role.GetDiscordRoleType() == "Team")
                {
                    return true;
                }
            }
            return false;
        }

        public static DiscordRoles GetDiscordRoleById(ulong id)
        {
            foreach (DiscordRoles role in Enum.GetValues(typeof(DiscordRoles)))
            {
                if ((ulong)role == id)
                {
                    return role;
                }
            }
            return DiscordRoles.None;
        }
    }

    public static class DiscordRoleExtensions
    {
        public static string ToRoleString(this DiscordRoles role)
        {
            // Get the [Description] category (e.g., "Team", "Rewards", "Cosmetic")
            var category = role.GetType()
                .GetField(role.ToString())
                .GetCustomAttributes(typeof(DescriptionAttribute), false)
                .Cast<DescriptionAttribute>()
                .FirstOrDefault()?.Description ?? "";

            string name = role.ToString();

            // Replace underscores with spaces
            name = name.Replace("_", " ");

            // Lowercase for normalization
            name = name.ToLowerInvariant();

            // Category-based formatting
            switch (category)
            {
                case "Cosmetic":
                case "Rewards":
                case "Playtime":
                    return CapitalizeWords(name);

                case "Team":
                case "Misc":
                default:
                    return FormatTeamRole(name);
            }
        }

        private static string CapitalizeWords(string input)
        {
            return string.Join(" ",
                input
                    .Split(" ", StringSplitOptions.RemoveEmptyEntries)
                    .Select(w => char.ToUpper(w[0]) + w.Substring(1))
            );
        }

        private static string FormatTeamRole(string name)
        {
            var words = name.Split(" ", StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < words.Length; i++)
            {
                switch (words[i])
                {
                    case "jr":
                    case "jr.":
                    case "jr_":
                        words[i] = "Jr";
                        break;

                    case "admin":
                        words[i] = "Admin";
                        break;

                    case "teamleitung":
                        words[i] = "Teamleitung";
                        break;

                    case "devleitung":
                        words[i] = "Devleitung";
                        break;

                    case "mod":
                        words[i] = "Mod";
                        break;

                    case "supporter":
                        words[i] = "Supporter";
                        break;

                    case "ek":
                        words[i] = "Ethikkomitee";
                        break;

                    default:
                        words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
                        break;
                }
            }

            return string.Join(" ", words);
        }
    }

}
