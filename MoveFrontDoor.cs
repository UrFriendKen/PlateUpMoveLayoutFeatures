using Kitchen;
using Kitchen.Layouts;
using KitchenData;
using KitchenMods;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KitchenMoveLayoutFeatures
{
    public class MoveFrontDoor : GameSystemBase, IModSystem
    {
        private struct SMoveRequest : IComponentData, IModComponent
        {
            public int Pos;
        }

        private static MoveFrontDoor _instance;

        private EntityQuery Appliances;

        protected override void Initialise()
        {
            base.Initialise();
            _instance = this;
            RequireSingletonForUpdate<SMoveRequest>();
            Appliances = GetEntityQuery(typeof(CAppliance), typeof(CPosition));
        }

        protected override void OnUpdate()
        {
            int pos = GetSingleton<SMoveRequest>().Pos;

            bool shouldChange = Has<SKitchenMarker>() &&
                Has<SIsNightTime>() &&
                Bounds.Contains(new Vector3(pos, 0f, GetFrontDoor().y));


            if (shouldChange && RequireEntity<SLayout>(out Entity layoutEntity) &&
                Require(layoutEntity, out CFrontDoorMarker frontDoorMarker) &&
                Mathf.Abs(frontDoorMarker.Location.x - pos) > 0.001f &&
                RequireBuffer(layoutEntity, out DynamicBuffer<CLayoutFeature> features))
            {
                bool frontDoorFound = false;
                for (int i = 0; i < features.Length; i++)
                {
                    CLayoutFeature feature = features[i];
                    if (feature.Type != FeatureType.FrontDoor)
                        continue;
                    feature.Tile1.x = pos;
                    feature.Tile2.x = pos;
                    features[i] = feature;
                    frontDoorFound = true;
                    break;
                }

                if (frontDoorFound)
                {
                    Vector3 oldExternalTile = frontDoorMarker.Location + Vector3.back;

                    frontDoorMarker.Location.x = pos;
                    Set(layoutEntity, frontDoorMarker);

                    Vector3 newExternalTile = frontDoorMarker.Location + Vector3.back;

                    if (TryGetSingletonEntity<SQueueMarker>(out Entity queueMarkerEntity) &&
                        Require(queueMarkerEntity, out CHasIndicator hasQueueIndicator) &&
                        Has<CPosition>(hasQueueIndicator.Indicator))
                    {
                        Set(hasQueueIndicator.Indicator, new CPosition(frontDoorMarker.Location + (Vector3.up * 0.5f)));
                    }

                    if (RequireBuffer(layoutEntity, out DynamicBuffer<CLayoutAppliancePlacement> layoutAppliances))
                    {
                        using NativeArray<Entity> applianceEntities = Appliances.ToEntityArray(Allocator.Temp);
                        using NativeArray<CAppliance> appliances = Appliances.ToComponentDataArray<CAppliance>(Allocator.Temp);
                        using NativeArray<CPosition> appliancePositions = Appliances.ToComponentDataArray<CPosition>(Allocator.Temp);

                        HashSet<Entity> movedAppliances = new HashSet<Entity>();

                        float doorZ = frontDoorMarker.Location.z - 0.5f;
                        float streetWallZ = frontDoorMarker.Location.z - 0.7f;
                        float externalZ = frontDoorMarker.Location.z - 1f;
                        float endOfStreetZ = frontDoorMarker.Location.z - 1.5f;
                        for (int i = 0; i < layoutAppliances.Length; i++)
                        {
                            CLayoutAppliancePlacement layoutAppliance = layoutAppliances[i];

                            bool shouldMove = false;
                            Vector3 fromPosition = default;
                            Vector3 toPosition = default;

                            if (layoutAppliances[i].Appliance == AssetReference.Nameplate)
                            {
                                Vector3 newNameplatePosition = (frontDoorMarker.Location.x < 3f) ? (newExternalTile + Vector3.right) : (newExternalTile - Vector3.right);
                                fromPosition = layoutAppliance.Position;
                                toPosition = newNameplatePosition;
                                shouldMove = true;
                            }
                            else if (Mathf.Abs(layoutAppliance.Position.z - doorZ) < 0.001f ||
                                Mathf.Abs(layoutAppliance.Position.z - streetWallZ) < 0.001f ||
                                Mathf.Abs(layoutAppliance.Position.z - externalZ) < 0.001f ||
                                layoutAppliance.Position.z < endOfStreetZ)
                            {
                                if (Mathf.Abs(layoutAppliance.Position.x - newExternalTile.x) < 0.001f)
                                {
                                    fromPosition = newExternalTile;
                                    toPosition = oldExternalTile;
                                    shouldMove = true;
                                }
                                else if (Mathf.Abs(layoutAppliance.Position.x - oldExternalTile.x) < 0.001f)
                                {
                                    fromPosition = oldExternalTile;
                                    toPosition = newExternalTile;
                                    shouldMove = true;
                                }
                            }

                            if (!shouldMove)
                                continue;

                            Entity applianceToMove = default;
                            CPosition applianceToMovePosition = default;
                            for (int j = 0; j < applianceEntities.Length; j++)
                            {
                                if (appliances[j].ID != layoutAppliance.Appliance ||
                                    movedAppliances.Contains(applianceEntities[j]) ||
                                    (appliancePositions[j].Position - layoutAppliance.Position).Chebyshev() > 0.001f)
                                    continue;
                                applianceToMove = applianceEntities[j];
                                applianceToMovePosition = appliancePositions[j];
                                break;
                            }

                            if (applianceToMove == default)
                                continue;

                            Vector3 offset = toPosition - fromPosition;

                            movedAppliances.Add(applianceToMove);

                            layoutAppliance.Position += offset;
                            layoutAppliances[i] = layoutAppliance;

                            applianceToMovePosition += offset;
                            Set(applianceToMove, applianceToMovePosition);
                            Main.LogInfo($"Moved {(GameData.Main.TryGet(layoutAppliance.Appliance, out Appliance appliance) ? appliance.name : $"Unknown ({layoutAppliance.Appliance})")} layout appliance");
                        }
                    }
                }
            }
            Clear<SMoveRequest>();
        }

        public static void SetPosition(int x)
        {
            _instance?.Set(new SMoveRequest()
            {
                Pos = x
            });
        }

        public static void MoveLeft()
        {
            Move(-1);
        }

        public static void MoveRight()
        {
            Move(1);
        }

        private static void Move(int offset)
        {
            _instance?.Set(new SMoveRequest()
            {
                Pos = Mathf.RoundToInt(_instance.GetFrontDoor().x) + offset
            });
        }
    }
}
