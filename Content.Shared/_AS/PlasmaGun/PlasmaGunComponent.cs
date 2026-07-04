using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Atmos;


namespace Content.Shared._AS.PlasmaGun;

/// <summary>
/// This is a rejigged implementation of the PneumaticCannonComponent, tailored to work for a series of plasma guns
/// Taking inspiration from @TaoNewt on Github and their Chem-lasers PR(#39231)
/// </summary>

[RegisterComponent, NetworkedComponent]

public sealed partial class PlasmaGunComponent : Component
{
    public const string TankSlotId = "gas_tank";

    /// <summary>
    ///     Amount of moles to consume for each shot at any power.
    /// </summary>
    [DataField("GasUsage")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float GasUsage = 0.142f;

    /// <summary>
    ///     Base projectile speed at default power.
    /// </summary>
    [DataField("baseProjectileSpeed")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float BaseProjectileSpeed = 20f;

    /// <summary>
    ///     The current projectile speed setting.
    /// </summary>
    [DataField]
    public float? ProjectileSpeed;

    /// <summary>
    ///     A hash of gases that the entity with this component can use to fire
    /// </summary>
    [DataField("AllowedGases")]
    public HashSet<Gas>? AllowedGases;

    /// <summary>
    ///     localisation string for when there is not enough gas of accepted type(s) in the tank, and it is ejected.
    /// </summary>
    [DataField]
    public LocId MessageGasLow = "tank-eject-gas-low";

    /// <summary>
    ///     localisation string for when there are impurities in the tank, and it is ejected.
    /// </summary>
    [DataField]
    public LocId MessageGasImpure = "tank-eject-impure";
}
