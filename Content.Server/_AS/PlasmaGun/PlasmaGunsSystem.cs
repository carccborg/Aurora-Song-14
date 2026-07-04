using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Storage.EntitySystems;
using Content.Server.Stunnable;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared._AS.PlasmaGun;
using Content.Shared.PneumaticCannon;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Tools.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;

namespace Content.Server._AS.PlasmaGun;

// This is essentially a more specialised implementation of the PneumaticCannonSystem to be used with plasma guns, avoiding
// the use of certain features (like self stun on high power, throwing items)
// Similar implementation has occurred upstream for chem-lasers, and solves a few problems so we're taking some ideas.
// Credit to @TaoNewt on GitHub in PR #39231 on Space Station 14 for the chem-laser implementation.

public sealed class PlasmaGunsSystem : SharedPlasmaGunsSystem
{
    [Dependency] private readonly GasTankSystem _gasTank = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlasmaGunComponent, GunShotEvent>(OnShoot);
        SubscribeLocalEvent<PlasmaGunComponent, ContainerIsInsertingAttemptEvent>(OnContainerInserting);
        SubscribeLocalEvent<PlasmaGunComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
    }

    private void OnContainerInserting(EntityUid uid, PlasmaGunComponent component, ContainerIsInsertingAttemptEvent args)
    {
        if (args.Container.ID != PlasmaGunComponent.TankSlotId)
            return;

        if (!TryComp<GasTankComponent>(args.EntityUid, out var tank))
            return;

        // Check that the tank has the required amount of required gas in it, rather than just any gas. Ejects if mixture is impure
        // Check that the AllowedGases hashset is not empty
        if (component.AllowedGases != null)
        {
            foreach (var gas in tank.Air) //Iterate over each gas present in the tank's mixture
            {
                if (!component.AllowedGases.Contains(gas.gas) && gas.moles > 0f) // If the current gas is not in the allowed hashset, eject
                {
                    _popups.PopupEntity(Loc.GetString(component.MessageGasImpure), uid, Transform(args.EntityUid).ParentUid);
                    args.Cancel();
                    return;
                }
            }
        }

        // Original implementation for only plasma
        // if (gas.Air.GetMoles(Gas.Plasma) < component.GasUsage)
        // {
        //     _popups.PopupEntity(Loc.GetString(component.));
        //     args.Cancel();
        //     return;
        // }
        //
        // // only accept tanks if it uses gas
        // if (gas.Air.TotalMoles >= component.GasUsage && component.GasUsage > 0f)
        //     return;
        if (component.GasUsage > 0f && component.GasUsage <= tank.Air.TotalMoles)
            return;

        _popups.PopupEntity(Loc.GetString(component.MessageGasLow), uid, Transform(args.EntityUid).ParentUid);
        args.Cancel();
    }

    private void OnShoot(Entity<PlasmaGunComponent> cannon, ref GunShotEvent args)
    {
        var (uid, component) = cannon;
        // require a gas tank if it uses gas
        var gas = GetGas(cannon);
        if (gas == null && component.GasUsage > 0f)
            return;

        // ignore gas stuff if the cannon doesn't use any
        if (gas == null)
            return;

        // Check the tank has enough plasma
        // This check might be pointless with newer implementation
        // if (gas.Value.Comp.Air.GetMoles(Gas.Plasma) < component.GasUsage)
        // {
        //     // Eject the tank if it no longer has enough plasma
        //     _slots.TryEject(uid, PlasmaGunComponent.TankSlotId, args.User, out _);
        //     return;
        // }

        // Not the most elegant solution, but it removes the gas without poisoning everyone nearby. Take it or leave it.
        // TODO: Maybe make a hashset to transpose plasma into proper offgas and expel that, implying it's actually been used
        var removed = _gasTank.RemoveAir(gas.Value, component.GasUsage);

        // Just in case we wanted to use the pneumatic cannon way of polluting local atmos with plasma, would be funny
        // var environment = _atmos.GetContainingMixture(cannon.Owner, false, true);
        // var removed = _gasTank.RemoveAir(gas.Value, component.GasUsage);
        // if (environment != null && removed != null)
        // {
        //     _atmos.Merge(environment, removed);
        // }

        if (gas.Value.Comp.Air.TotalMoles >= component.GasUsage)
            return;

        // eject gas tank, with a popup
        _slots.TryEject(uid, PlasmaGunComponent.TankSlotId, args.User, out _);
        _popups.PopupEntity(Loc.GetString(component.MessageGasLow), uid);
    }

    private void OnGunRefreshModifiers(Entity<PlasmaGunComponent> ent, ref GunRefreshModifiersEvent args)
    {
        if (ent.Comp.ProjectileSpeed is { } speed)
            args.ProjectileSpeed = speed;
    }

    /// <summary>
    ///     Returns whether the plasma gun has enough gas to shoot, as well as the tank itself.
    /// </summary>
    private Entity<GasTankComponent>? GetGas(EntityUid uid)
    {
        if (!Container.TryGetContainer(uid, PlasmaGunComponent.TankSlotId, out var container) ||
            container is not ContainerSlot slot || slot.ContainedEntity is not {} contained)
            return null;

        return TryComp<GasTankComponent>(contained, out var gasTank) ? (contained, gasTank) : null;
    }
}
