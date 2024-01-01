using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using VanillaPlusFramework.TemplateClasses;
using System.Diagnostics.Eventing.Reader;

namespace VanillaPlusFramework.Utilities
{
    public class FrameworkUtilities
    {
        #region EMP, JDI, SCI Logics
        // EMP Logic
        public static void EMP(Vector3D Position, float EMP_Radius, int EMP_TimeDisabled)
        {
            BoundingSphereD EMP_Sphere = new BoundingSphereD(Position, EMP_Radius);
            List<IMyEntity> DetectedEntities = new List<IMyEntity>(MyAPIGateway.Entities.GetEntitiesInSphere(ref EMP_Sphere));

            foreach (var entity in DetectedEntities)
            {
                if (entity != null && entity is IMyCubeGrid)
                {
                    IMyCubeGrid cubegrid = (IMyCubeGrid)entity;

                    List<IMySlimBlock> BlocksToEMP = new List<IMySlimBlock>(cubegrid.GetBlocksInsideSphere(ref EMP_Sphere));

                    foreach (IMySlimBlock SlimBlock in BlocksToEMP)
                    {
                        IMyCubeBlock cubeBlock = SlimBlock.FatBlock;
                        if (cubeBlock is IMyFunctionalBlock)
                        {
                            IMyFunctionalBlock FunctionalBlock = cubeBlock as IMyFunctionalBlock;
                            DisabledBlock BlockData;
                            if (VanillaPlusFramework.DisabledBlocks.TryGetValue(FunctionalBlock.EntityId, out BlockData))
                            {
                                BlockData.TicksDisabled += EMP_TimeDisabled / 2;
                            }
                            else
                            {
                                VanillaPlusFramework.DisabledBlocks.Add(FunctionalBlock.EntityId, new DisabledBlock(FunctionalBlock.Enabled, EMP_TimeDisabled));

                                FunctionalBlock.Enabled = false;
                            }
                        }
                    }
                }
            }
        }

        // JDI logic
        public static void JDI_Hit(MyEntity hitEntity, float JDI_PowerDrainInW, bool JDI_DistributePower)
        {
            IMyCubeGrid hitGrid = null;

            if (hitEntity is IMyCubeGrid)
            {
                hitGrid = hitEntity as IMyCubeGrid;
            }
            else if (hitEntity is IMySlimBlock)
            {
                IMySlimBlock block = hitEntity as IMySlimBlock;
                hitGrid = block.CubeGrid;
            }
            else if (hitEntity is IMyCubeBlock)
            {
                IMyCubeBlock block = hitEntity as IMyCubeBlock;
                hitGrid = block.CubeGrid;
            }

            if (hitGrid != null)
            {
                List<IMyJumpDrive> jumpDrives = new List<IMyJumpDrive>();
                foreach (IMyJumpDrive drive in hitGrid.GetFatBlocks<IMyJumpDrive>())
                {
                    jumpDrives.Add(drive);
                }

                if (JDI_DistributePower == true)
                {
                    JDI_PowerDrainInW /= jumpDrives.Count;
                }

                JDI_PowerDrainInW /= 1000000;

                foreach (IMyJumpDrive jumpDrive in jumpDrives)
                {

                    if (jumpDrive.CurrentStoredPower - JDI_PowerDrainInW > 0)
                    {
                        jumpDrive.CurrentStoredPower -= JDI_PowerDrainInW;
                    }
                    else
                    {
                        jumpDrive.CurrentStoredPower = 0;
                    }
                }
            }
        }

        // Special Componentry Interaction Logic
        public static void SCI_Hit(MyEntity hitEntity, List<SpecialComponentryInteraction_Logic> SCI_Stats, Vector3 hitpos)
        {
            IMyCubeGrid hitGrid = null;


            if (hitEntity is IMyCubeGrid)
            {
                hitGrid = hitEntity as IMyCubeGrid;
            }
            else if (hitEntity is IMySlimBlock)
            {
                IMySlimBlock block = hitEntity as IMySlimBlock;
                hitGrid = block.CubeGrid;
            }
            else if (hitEntity is IMyCubeBlock)
            {
                IMyCubeBlock block = hitEntity as IMyCubeBlock;
                hitGrid = block.CubeGrid;
            }

            if (hitGrid != null)
            {
                List<IMySlimBlock> slimblocks = new List<IMySlimBlock>();
                hitGrid.GetBlocks(slimblocks);
                List<IMyCubeBlock> blocks = new List<IMyCubeBlock>();

                foreach (IMySlimBlock slimblock in slimblocks)
                {
                    blocks.Add(slimblock.FatBlock);
                }

                for (int i = 0; i < blocks.Count; i++)
                {
                    if (blocks[i] != null)
                    {
                        ApplySCI_Logic(blocks[i], SCI_Stats, hitpos);
                    }
                }

                blocks.Clear();
            }
        }
        public static T CastHax<T>(T typeRef, object value) => (T)value;

        private static bool AppliesToBlock(SpecialComponentryInteraction_Logic logic, IMyCubeBlock block, Vector3 hitpos)
        {
            if (logic.SCI_BlockId == "*" || (logic.SCI_IdType == IdType.SubtypeId ? logic.SCI_BlockId == GetTrueSubtypeID(block.SlimBlock) : logic.SCI_BlockId == block.BlockDefinition.TypeId.ToString().Remove(0, 16)))
            {
                return logic.SCI_Radius > 0 ? Vector3.DistanceSquared(hitpos, block.PositionComp.GetPosition()) <= logic.SCI_Radius : true;
            }
            return false;
        }

        public static string GetTrueSubtypeID(IMySlimBlock block)
        {
            if (block.BlockDefinition.Id.SubtypeName == "")
            {
                return block.BlockDefinition.Id.TypeId.ToString().Remove(0, 16);
            }
            return block.BlockDefinition.Id.SubtypeName;
        }

        public static bool ApplySCI_Logic(IMyCubeBlock block, List<SpecialComponentryInteraction_Logic> SCI_Stats, Vector3 hitpos)
        {
            foreach (SpecialComponentryInteraction_Logic logic in SCI_Stats)
            {
                if (AppliesToBlock(logic, block, hitpos))
                {
                    if (block is IMyFunctionalBlock && logic.SCI_DisableTime > 0)
                    {
                        IMyFunctionalBlock FunctionalBlock = block as IMyFunctionalBlock;

                        DisabledBlock BlockData;
                        if (VanillaPlusFramework.DisabledBlocks.TryGetValue(FunctionalBlock.EntityId, out BlockData))
                        {
                            BlockData.TicksDisabled += (int)(logic.SCI_DisableTime / 2);
                        }
                        else
                        {
                            VanillaPlusFramework.DisabledBlocks.Add(FunctionalBlock.EntityId, new DisabledBlock(FunctionalBlock.Enabled, (int)logic.SCI_DisableTime));

                            FunctionalBlock.Enabled = false;
                        }
                    }

                    IMySlimBlock slimblock = block.SlimBlock;
                    if (logic.SCI_DamageType == DamageType.Percent)
                    {
                        float Damage = slimblock.MaxIntegrity;
                        Damage *= logic.SCI_DamageDealt / 100;
                        slimblock.DoDamage(Damage, MyStringHash.NullOrEmpty, true, null, 0);

                    }
                    else
                    {

                        slimblock.DoDamage(logic.SCI_DamageDealt, MyStringHash.NullOrEmpty, true, null, 0);
                    }

                    if (block == null)
                        return true;

                    return false;
                }
            }
            return false;
        }
        #endregion
    }
}
