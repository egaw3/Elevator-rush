using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// The home tower (home plan 3.2): built with the level building code, one floor per unlocked
    /// floor plus a construction site for the next one, with the cabin idling up and down. The
    /// camera uses the Home screen's UI bands and refits whenever the tower grows.
    /// </summary>
    public sealed class HomeController : MonoBehaviour
    {
        GameConfig config;
        Building building;
        ElevatorController elevator;
        PassengerSpawner spawner;
        Environment environment;
        CameraRig cameraRig;

        public int FloorCount { get; private set; }
        public bool Visible { get; private set; }
        public bool HasConstructionSite => building.HasConstructionSite;
        public Vector3 SignPosition => building.ConstructionSignPosition;
        /// <summary>Levels completed at which the construction floor opens.</summary>
        public int NextFloorAt { get; private set; }

        public void Init(GameConfig config, Building building, ElevatorController elevator, PassengerSpawner spawner,
            Environment environment, CameraRig cameraRig)
        {
            this.config = config;
            this.building = building;
            this.elevator = elevator;
            this.spawner = spawner;
            this.environment = environment;
            this.cameraRig = cameraRig;
        }

        /// <summary>Builds the home tower for this progress, early-unlocked floors included (decor comes from the building's DecorSource) and frames it.</summary>
        public void Show(SaveData save, int themeIndex, bool instantCamera)
        {
            Visible = true;
            FloorCount = UnlockSchedule.UnlockedFloorCount(save);
            bool construction = FloorCount < Catalog.FloorCount;
            NextFloorAt = construction ? Catalog.FloorUnlockAt(FloorCount) : 0;

            spawner.Clear();
            building.Build(FloorCount, themeIndex, construction);
            environment.Apply(Theme.Get(themeIndex), building.LeftX, building.RightX, building.FrontZ, building.BackZ);
            elevator.AttachTo(building);
            elevator.StartIdle(FloorCount);

            cameraRig.SetBands(config.homeCameraTopBand, config.homeCameraBottomBand);
            cameraRig.Fit(building.Bounds, instantCamera);
        }

        /// <summary>Frames the whole tower again (leaving Build mode).</summary>
        public void ShowTower()
        {
            cameraRig.SetBands(config.homeCameraTopBand, config.homeCameraBottomBand);
            cameraRig.Fit(building.Bounds, false);
        }

        /// <summary>Moves the camera in on one floor, keeping it clear of a bottom UI band (home plan 5.3).</summary>
        public void FocusFloor(int floor, float bottomBand)
        {
            cameraRig.SetBands(config.homeCameraTopBand, bottomBand);
            cameraRig.Fit(building.FloorBounds(floor), false);
        }

        /// <summary>Moves the camera in on any world box (the Characters preview), clear of a bottom UI band.</summary>
        public void FocusBox(Bounds box, float bottomBand, float maxScreenFraction)
        {
            cameraRig.SetBands(config.homeCameraTopBand, bottomBand, maxScreenFraction);
            cameraRig.Fit(box, false);
        }

        /// <summary>Called when a level takes over the building; the level code rebuilds it.</summary>
        public void Hide()
        {
            Visible = false;
            cameraRig.SetBands(config.cameraTopBand, config.cameraBottomBand);
        }
    }
}
