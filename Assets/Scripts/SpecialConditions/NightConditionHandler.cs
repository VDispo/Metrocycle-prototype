using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NightConditionHandler : ConditionHandler
{
    public override string ConditionName { get => "Night"; }

    /// TODO
    /// - replace sun
    /// - activate light sources (maybe via tag or layer or custimized script)
    /// - maybe add thicc vignette
}
