using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace SEPC.Extensions
{
    public static class IMyEntityExtensions
    {
        public static string BestName(this IMyEntity entity)
        {
            IMyCubeBlock asBlock = entity as IMyCubeBlock;
            string name;
            if (asBlock != null)
            {
                name = asBlock.DisplayNameText;
                if (string.IsNullOrEmpty(name))
                    name = asBlock.DefinitionDisplayNameText;
                return name;
            }

            if (entity == null)
                return "N/A";

            name = entity.DisplayName;
            if (string.IsNullOrEmpty(name))
            {
                name = entity.Name;
                if (string.IsNullOrEmpty(name))
                {
                    name = entity.GetFriendlyName();
                    if (string.IsNullOrEmpty(name))
                    {
                        name = entity.ToString();
                    }
                }
            }
            return name;
        }

        public static string NameWithId(this IMyEntity entity)
        {
            return entity == null ? "N/A" : BestName(entity) + '(' + entity.EntityId + ')';
        }
    }
}
