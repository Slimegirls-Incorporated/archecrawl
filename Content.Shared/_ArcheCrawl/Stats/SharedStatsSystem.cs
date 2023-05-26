﻿using Content.Shared._ArcheCrawl.Stats.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._ArcheCrawl.Stats;

/// <summary>
/// This handles modifying and accessing <see cref="StatPrototype"/>
/// values from <see cref="StatsComponent"/>
/// </summary>
public abstract partial class SharedStatsSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;

    private ISawmill _sawmill = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        InitializeScaling();

        SubscribeLocalEvent<StatsComponent, MapInitEvent>(OnMapInit);

        _sawmill = Logger.GetSawmill("stat");
    }

    private void OnMapInit(EntityUid uid, StatsComponent component, MapInitEvent args)
    {
        foreach (var (key, val) in component.InitialStats)
        {
            component.Stats[key] = default;
            SetStatValue(uid, key, val, component);
        }
        Dirty(component);
    }

    #region Stat Setters
    [PublicAPI]
    public void ModifyStatValue(EntityUid uid, string stat, int delta, StatsComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!component.Stats.TryGetValue(stat, out var val))
            return;

        SetStatValue(uid, stat, val + delta, component);
    }

    [PublicAPI]
    public void ModifyStatValue(EntityUid uid, StatPrototype stat, int delta, StatsComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!component.Stats.TryGetValue(stat.ID, out var val))
            return;

        SetStatValue(uid, stat, val + delta, component);
    }

    public void SetStatValue(EntityUid uid, string stat, int value, StatsComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!PrototypeManager.TryIndex<StatPrototype>(stat, out var statPrototype))
        {
            _sawmill.Error($"Invalid stat prototype ID: \"{stat}\"");
            return;
        }

        SetStatValue(uid, statPrototype, value, component);
    }

    public void SetStatValue(EntityUid uid, StatPrototype stat, int value, StatsComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!component.Stats.ContainsKey(stat.ID))
            return;

        var ev = new StatChangedEvent(uid, stat, component.Stats[stat.ID], value);
        component.Stats[stat.ID] = Math.Clamp(value, stat.MinValue, stat.MaxValue);
        Dirty(component);
        RaiseLocalEvent(uid, ref ev, true);
        RaiseNetworkEvent(new NetworkStatChangedEvent(ev), uid);
    }
    #endregion

    #region Stat Getters
    [PublicAPI]
    public int GetStat(EntityUid uid, StatPrototype stat, StatsComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return 0;

        return GetStat(uid, stat.ID, component);
    }

    public int GetStat(EntityUid uid, string stat, StatsComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return 0;

        if (!component.Stats.ContainsKey(stat))
            return 0;

        return component.Stats.GetValueOrDefault(stat);
    }
    #endregion
}
