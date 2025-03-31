using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RainConditionHandler : ConditionHandler
{
    public override string ConditionName { get => "Rain"; }

    //[Header("Vehicle")]
    [HideInInspector] public bool isMotorcycle = false;
    [HideInInspector] public Transform activeVehicle;

    [Header("Rain Particles")]
    [SerializeField] private Transform rainParticlesTransform;
    [SerializeField][Tooltip("interval [in seconds] of updating rain particle position to follow player vehicle")] private float particlesPositionUpdateInterval = 1; 
    private float startOffsetZ = 0; // Z of particle's center is offset forward since the player sees more things in the forward direction (its better to put more particles there than at the back)

    private void Start()
    {
        startOffsetZ = rainParticlesTransform.localPosition.z;
        StartCoroutine(nameof(UpdateRainPosition));
    }

    private void Update()
    {
        Skid();
    }

    private IEnumerator UpdateRainPosition()
    {
        rainParticlesTransform.localPosition = new Vector3(
            activeVehicle.localPosition.x,
            rainParticlesTransform.localPosition.y,
            activeVehicle.localPosition.z + startOffsetZ);
        yield return new WaitForSecondsRealtime(particlesPositionUpdateInterval);
        StartCoroutine(nameof(UpdateRainPosition));
    }

    // this might need to be implemented inside ArcadeBikeController.cs and BicycleVehicle.cs
    private void Skid()
    {
        // not sure abt this part
        // is it just: theres a % chance that when u break in one go, [what is da % chance]
        // that you wouldnt stop cuz u'd be skidding,
        // then play skidding sound,
        // then if too much, show skidding prompt and restart to last checkpoint

        // so like:
        // if speed > x (unknown speed x for now)
        // AND brake was held for n milliseconds (unknown tolerance n for now)
        // AND rolled for the % skid chance (unknown % for now),
        // THEN % of original speed is kept (unknown % for now) as skidding speed
        // AND play skidding sound
        // AND cant (?) use any controls other than brakes (?)
        // AND programmatically remember this was a skid (so next prompts could reference "u bumped to someone cuz u skid'd"
        // AND if bumped, do popup the modified (skid) prompt
        // AND restart to last checkpoint

        // idea ni balong:
        // less turn angle the faster it is
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}
