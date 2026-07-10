using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Watcher;

namespace RainMeadow
{
    public static class StoryHelpers
    {
        public static void SaveEchoWarp(RainWorldGame game, WarpPoint warpPoint, bool saveRoomWarp = false, bool saveString = false, bool initiateWarp = false)
        {
            var warpData = warpPoint.overrideData ?? warpPoint.Data;
            if (saveRoomWarp)
            {
                RainMeadow.Info("Trying to spawn echo warp point in room");
                if (warpPoint.room != null)
                    warpPoint.room.TrySpawnWarpPoint(warpPoint.placedObject);
                else RainMeadow.Info("Failed due to null room");
            }
            if (saveString) game.GetStorySession.spinningTopWarpsLeadingToRippleScreen.Add(warpPoint.MyIdentifyingString());
            game.GetStorySession.saveState.warpPointTargetAfterWarpPointSave = warpData;
            if (RainMeadow.isStoryMode(out var storyGameMode)) storyGameMode.myLastWarp = warpData;
            if (initiateWarp)
                ForceLoadDesiredWarp(game.overWorld, warpPoint, warpData, true, false);
        }
        public static void ForceLoadDesiredWarp(OverWorld overWorld, WarpPoint warpPoint, WarpPoint.WarpPointData warpData, bool useNormalWarpLoader, bool changeCamera = true)
        {
            if (RainMeadow.isStoryMode(out var story)) story.myLastWarp = warpData;
            overWorld.InitiateSpecialWarp_WarpPoint(warpPoint, warpData, useNormalWarpLoader);

            // emulate as if we did actually warp
            if (changeCamera)
            {
                string destRoom = warpData.destRoom;
                var destCam = warpData.destCam;
                overWorld.game.cameras[0].WarpMoveCameraPrecast(destRoom, destCam);
                RainMeadow.Debug($"switch camera to {destRoom}");

                if (overWorld.game.cameras[0].warpPointTimer == null)
                {
                    overWorld.game.cameras[0].warpPointTimer = new Watcher.WarpPoint.WarpPointTimer(warpPoint.activateAnimationTime * 2f, warpPoint);
                    overWorld.game.cameras[0].warpPointTimer.MoveToSecondHalf();
                    overWorld.game.cameras[0].BlankWarpPointHoldFrame();
                }
            }

            warpPoint.activated = false;
            overWorld.readyForWarp = !useNormalWarpLoader;


        }
        public static Watcher.WarpPoint? PerformWarpHelper(string? sourceRoomName, string warpData, bool useNormalWarpLoader, bool hackFixRoom)
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return null;
            RainMeadow.Info($"Warp point? in {sourceRoomName}; data={warpData}, Loader={useNormalWarpLoader}");
            // generate "local" warp point
            Watcher.WarpPoint.WarpPointData newWarpData = new(null);
            newWarpData.FromString(warpData);
            PlacedObject placedObject = new(PlacedObject.Type.WarpPoint, newWarpData);
            Watcher.WarpPoint warpPoint = new(null, placedObject);

            /*if (hackFixRoom)
            {
                foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
                { //it will move places
                    if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                    if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo1 && opo1.isMine && opo1.apo is AbstractCreature ac)
                    {
                        if (ac.Room.realizedRoom == null)
                        {
                            RainMeadow.Error("something very evil happened, but let's try not to worry about it");
                            ac.Room.RealizeRoom(ac.Room.world, game);
                        }
                        warpPoint.room = ac.Room.realizedRoom;
                    }
                }
            }
            else
            {*/
            if (sourceRoomName is not null)
            {
                RainMeadow.Info($"Trying to find room {sourceRoomName} to set false warp point in");
                var abstractRoom2 = game.overWorld.activeWorld.GetAbstractRoom(sourceRoomName);
                if (abstractRoom2.realizedRoom == null)
                {
                    if (game.roomRealizer != null)
                    {
                        game.roomRealizer = new RoomRealizer(game.roomRealizer.followCreature, game.overWorld.activeWorld);
                    }
                    abstractRoom2.RealizeRoom(game.overWorld.activeWorld, game);
                }
                // do nat throw everyone into the same room?
                warpPoint.room = abstractRoom2.realizedRoom;
                RainMeadow.Info($"Room {(warpPoint.room == null? "fail" : "success")}");
            }
            //}
            if (!OnlineManager.lobby.isOwner)
                ForceLoadDesiredWarp(game.overWorld, warpPoint, warpPoint.overrideData ?? warpPoint.Data, useNormalWarpLoader);
            return warpPoint;
        }
    }
}
