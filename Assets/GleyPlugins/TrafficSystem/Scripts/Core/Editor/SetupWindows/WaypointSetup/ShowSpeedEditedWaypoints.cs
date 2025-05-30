﻿using GleyUrbanAssets;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GleyTrafficSystem
{
    public class ShowSpeedEditedWaypoints : ShowWaypointsTrafficBase
    {
        public override ISetupWindow Initialize(WindowProperties windowProperties, SettingsWindowBase window)
        {
            base.Initialize(windowProperties, window);
            save = settingsLoader.LoadSpeedEditedWaypointsSave();
            return this;
        }


        protected override void ScrollPart(float width, float height)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false, GUILayout.Width(width - SCROLL_SPACE), GUILayout.Height(height - scrollAdjustment));
            base.ScrollPart(width, height);
            GUILayout.EndScrollView();
        }


        public override void DestroyWindow()
        {
            settingsLoader.SaveSpeedEditedWaypointsSettings(save, roadColors);
            base.DestroyWindow();
        }


        protected override List<WaypointSettingsBase> GetWaypointsOfIntereset()
        {
            return waypointDrawer.ShowSpeedEditedWaypoints(roadColors.waypointColor, save.showConnections, roadColors.waypointColor, save.showSpeed, roadColors.speedColor, save.showCars, roadColors.carsColor, save.showOtherLanes, roadColors.laneChangeColor).Cast<WaypointSettingsBase>().ToList();

        }
    }
}
