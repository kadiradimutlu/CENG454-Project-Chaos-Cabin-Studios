using Fusion;
using UnityEngine;
 
/// <summary>
/// Island haritasındaki zehir tuzağı.
/// İki bağımsız bölge (Zone A ve Zone B) içerir; her biri ayrı bir TrapButton ile tetiklenir.
/// Her bölgede bir BoxCollider + DamageVolume child'ı bulunmalıdır.
///
/// Prefab hiyerarşisi:
/// PF_IslandPoisonZone
///   ├── PoisonZone_A          → BoxCollider (trigger) + DamageVolume
///   └── PoisonZone_B          → BoxCollider (trigger) + DamageVolume
///
/// Inspector bağlantıları:
///   Poison Zone A Object  → PoisonZone_A child objesi
///   Poison Zone B Object  → PoisonZone_B child objesi
///   Zone Type             → Island (menüden)
///   Duration              → kaç saniye aktif kalacağı (örn. 5)
///   Cooldown              → yeniden tetiklenene kadar bekleme (örn. 8)
/// </summary>
[DisallowMultipleComponent]
public class IslandPoisonZone : NetworkBehaviour
{
    // ── Inspector Slotları ────────────────────────────────────────────────
 
    [Header("Zone References")]
    [Tooltip("PoisonZone_A child objesini sürükle (BoxCollider + DamageVolume burada).")]
    [SerializeField] private GameObject poisonZoneA;
 
    [Tooltip("PoisonZone_B child objesini sürükle (BoxCollider + DamageVolume burada).")]
    [SerializeField] private GameObject poisonZoneB;
 
    [Header("Trap Settings")]
    [SerializeField] private ZoneType zoneType = ZoneType.Island;
 
    [Tooltip("Bölge kaç saniye aktif kalır.")]
    [SerializeField] private float duration = 5f;
 
    [Tooltip("Deaktivasyon sonrası tekrar tetiklenebilmek için bekleme süresi.")]
    [SerializeField] private float cooldown = 8f;
 
    [Header("Optional")]
    [Tooltip("Aktivasyon sırasında oynatılacak efekt/ses objesi (opsiyonel).")]
    [SerializeField] private GameObject activationVisualsA;
 
    [SerializeField] private GameObject activationVisualsB;
 
    // ── Networked State ────────────────────────────────────────────────────
    // Her bölge için bağımsız aktif durum; Render() bunları dinleyerek
    // collider'ları açıp kapar → tüm clientlar senkron görür.
 
    [Networked, OnChangedRender(nameof(OnZoneAChanged))]
    private NetworkBool ZoneAActive { get; set; }
 
    [Networked, OnChangedRender(nameof(OnZoneBChanged))]
    private NetworkBool ZoneBActive { get; set; }
 
    // ── Cooldown zamanlayıcıları (sadece StateAuthority'de çalışır) ────────
 
    private TickTimer _cooldownTimerA;
    private TickTimer _cooldownTimerB;
    private TickTimer _durationTimerA;
    private TickTimer _durationTimerB;
 
    // ── Fusion Lifecycle ──────────────────────────────────────────────────
 
    public override void Spawned()
    {
        // Spawn anında her iki bölge de kapalı olmalı.
        SetZoneAVisual(false);
        SetZoneBVisual(false);
 
        if (HasStateAuthority)
        {
            ZoneAActive = false;
            ZoneBActive = false;
        }
    }
 
    public override void FixedUpdateNetwork()
    {
        // Sadece StateAuthority süre/cooldown takibi yapar.
        if (!HasStateAuthority)
            return;
 
        // Zone A süresi doldu mu?
        if (ZoneAActive && _durationTimerA.Expired(Runner))
        {
            ZoneAActive = false;
            _cooldownTimerA = TickTimer.CreateFromSeconds(Runner, cooldown);
            Debug.Log($"[IslandPoisonZone] {name}: Zone A deactivated (duration expired). Cooldown started.", this);
        }
 
        // Zone B süresi doldu mu?
        if (ZoneBActive && _durationTimerB.Expired(Runner))
        {
            ZoneBActive = false;
            _cooldownTimerB = TickTimer.CreateFromSeconds(Runner, cooldown);
            Debug.Log($"[IslandPoisonZone] {name}: Zone B deactivated (duration expired). Cooldown started.", this);
        }
    }
 
    // ── Aktivasyon API'si (TrapButton tarafından çağrılır) ────────────────
 
    /// <summary>Zone A'yı tetikler. TrapButton'un "Poison Zone A" slotuna bağlı buton çağırır.</summary>
    public void ActivateZoneA()
    {
        if (!HasStateAuthority)
        {
            Debug.LogWarning($"[IslandPoisonZone] {name}: ActivateZoneA çağrıldı ama StateAuthority bu objede değil.", this);
            return;
        }
 
        if (ZoneAActive)
        {
            Debug.Log($"[IslandPoisonZone] {name}: Zone A zaten aktif.", this);
            return;
        }
 
        if (!_cooldownTimerA.ExpiredOrNotRunning(Runner))
        {
            Debug.Log($"[IslandPoisonZone] {name}: Zone A cooldown'da, tetiklenemiyor.", this);
            return;
        }
 
        ZoneAActive = true;
        _durationTimerA = TickTimer.CreateFromSeconds(Runner, duration);
        Debug.Log($"[IslandPoisonZone] {name}: Zone A ACTIVATED. Duration={duration}s", this);
    }
 
    /// <summary>Zone B'yi tetikler. TrapButton'un "Poison Zone B" slotuna bağlı buton çağırır.</summary>
    public void ActivateZoneB()
    {
        if (!HasStateAuthority)
        {
            Debug.LogWarning($"[IslandPoisonZone] {name}: ActivateZoneB çağrıldı ama StateAuthority bu objede değil.", this);
            return;
        }
 
        if (ZoneBActive)
        {
            Debug.Log($"[IslandPoisonZone] {name}: Zone B zaten aktif.", this);
            return;
        }
 
        if (!_cooldownTimerB.ExpiredOrNotRunning(Runner))
        {
            Debug.Log($"[IslandPoisonZone] {name}: Zone B cooldown'da, tetiklenemiyor.", this);
            return;
        }
 
        ZoneBActive = true;
        _durationTimerB = TickTimer.CreateFromSeconds(Runner, duration);
        Debug.Log($"[IslandPoisonZone] {name}: Zone B ACTIVATED. Duration={duration}s", this);
    }
 
    // ── OnChangedRender Callback'leri (tüm clientlarda çalışır) ──────────
 
    private void OnZoneAChanged()
    {
        SetZoneAVisual(ZoneAActive);
    }
 
    private void OnZoneBChanged()
    {
        SetZoneBVisual(ZoneBActive);
    }
 
    // ── Görsel/Collider Yönetimi ──────────────────────────────────────────
 
    private void SetZoneAVisual(bool active)
    {
        if (poisonZoneA != null)
            poisonZoneA.SetActive(active);
 
        if (activationVisualsA != null)
            activationVisualsA.SetActive(active);
    }
 
    private void SetZoneBVisual(bool active)
    {
        if (poisonZoneB != null)
            poisonZoneB.SetActive(active);
 
        if (activationVisualsB != null)
            activationVisualsB.SetActive(active);
    }
 
    // ── Editor Yardımcısı ─────────────────────────────────────────────────
 
    private void OnValidate()
    {
        duration = Mathf.Max(0.1f, duration);
        cooldown = Mathf.Max(0f, cooldown);
    }
}
