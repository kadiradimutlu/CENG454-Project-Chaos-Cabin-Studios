using Fusion;
using UnityEngine;
 

[DisallowMultipleComponent]
public class DesertMinefield : NetworkBehaviour
{
    [System.Serializable]
    public class MineEntry
    {
        [Tooltip("The mine object (cactus). Holds the DamageVolume + trigger collider + " +
                 "renderer. We DO NOT SetActive(false) this anymore (that would also hide " +
                 "single-object mines); instead we toggle its renderer + trigger collider.")]
        public GameObject damageObject;
 
        [Tooltip("Renderers turned off to make the mine invisible while armed. If left " +
                 "empty, renderers are auto-collected from damageObject at runtime.")]
        public Renderer[] visuals;
 
        [Tooltip("Trigger collider(s) enabled only while armed. If left empty, trigger " +
                 "colliders are auto-collected from damageObject at runtime.")]
        public Collider[] triggerColliders;
 
        
        [System.NonSerialized] public Renderer[] cachedRenderers;
        [System.NonSerialized] public Collider[] cachedColliders;
        [System.NonSerialized] public bool cached;
    }
 
    [Header("Zone")]
    [SerializeField] private ZoneType zoneType = ZoneType.Desert;
 
    [Header("Mines")]
    [SerializeField] private MineEntry[] mines;
 
    [Header("Timing")]
    [Tooltip("How long (seconds) the mines stay invisible & armed per trigger.")]
    [SerializeField] private float mineActiveDuration = 6f;
 
    [Tooltip("Seconds before the trap can be triggered again.")]
    [SerializeField] private float cooldown = 10f;
 
    // Tick at which the mines disarm/reappear. 0 = inactive (visible, disarmed).
    [Networked] private int ActiveUntilTick { get; set; }
    [Networked] private int CooldownUntilTick { get; set; }
 
    private bool lastActiveState;
 
    public bool IsActive => ActiveUntilTick != 0 && Runner != null && Runner.Tick < ActiveUntilTick;
    public bool IsOnCooldown => CooldownUntilTick != 0 && Runner != null && Runner.Tick < CooldownUntilTick;
 
    public float CooldownRemaining
    {
        get
        {
            if (Runner == null || !IsOnCooldown)
                return 0f;
 
            return (CooldownUntilTick - Runner.Tick) * Runner.DeltaTime;
        }
    }
 
    public override void Spawned()
    {
        Debug.Log($"[Minefield] {name}: SPAWNED. mines={(mines!=null?mines.Length:0)}, IsActive={IsActive}, ActiveUntilTick={ActiveUntilTick}", this);
 
        
        SetMineState(false);
        lastActiveState = false;
 
        // İlk mayının gerçek renderer durumu
        if (mines != null && mines.Length > 0 && mines[0] != null && mines[0].damageObject != null)
        {
            EnsureCached(mines[0]);
            int rc = mines[0].cachedRenderers != null ? mines[0].cachedRenderers.Length : 0;
            bool firstEnabled = rc > 0 && mines[0].cachedRenderers[0] != null && mines[0].cachedRenderers[0].enabled;
            int layer = mines[0].damageObject.layer;
            Debug.Log($"[Minefield] {name}: ilk mayın '{mines[0].damageObject.name}' renderer sayısı={rc}, ilk renderer.enabled={firstEnabled}, layer={layer} ({LayerMask.LayerToName(layer)})", mines[0].damageObject);
 
            // KAMERA TEŞHİSİ: aktif kameralar bu layer'ı çiziyor mu?
            Renderer rr = (rc > 0) ? mines[0].cachedRenderers[0] : null;
            if (rr != null)
            {
                Debug.Log($"[Minefield] {name}: ilk mayın renderer aktif mi (isVisible)={rr.isVisible}, gameObject.activeInHierarchy={mines[0].damageObject.activeInHierarchy}, material sayısı={rr.sharedMaterials.Length}, ilk material={(rr.sharedMaterial!=null?rr.sharedMaterial.name:"NULL")}", rr);
            }
 
            // İlk mayının TÜM renderer'larını listele (child dahil)
            Renderer[] allR = mines[0].damageObject.GetComponentsInChildren<Renderer>(true);
            Debug.Log($"[Minefield] {name}: ilk mayında TOPLAM {allR.Length} renderer (child dahil):", mines[0].damageObject);
            for (int i = 0; i < allR.Length; i++)
                Debug.Log($"[Minefield]     renderer[{i}] = '{allR[i].gameObject.name}', enabled={allR[i].enabled}, obj aktif={allR[i].gameObject.activeInHierarchy}", allR[i]);
 
            Camera[] cams = Camera.allCameras;
            Debug.Log($"[Minefield] {name}: aktif kamera sayısı={cams.Length}", this);
            foreach (Camera cam in cams)
            {
                bool sees = (cam.cullingMask & (1 << layer)) != 0;
                Debug.Log($"[Minefield]   - Kamera '{cam.name}': bu layer'ı çiziyor mu={sees}, enabled={cam.enabled}, depth={cam.depth}", cam);
            }
        }
    }
 
    
    public void Activate()
    {
        if (!Object.HasStateAuthority)
            return;
 
        if (IsOnCooldown)
            return; // silently ignored
 
        int durationTicks = Mathf.CeilToInt(Mathf.Max(0f, mineActiveDuration) / Runner.DeltaTime);
        int cooldownTicks = Mathf.CeilToInt(Mathf.Max(0f, cooldown) / Runner.DeltaTime);
 
        ActiveUntilTick = Runner.Tick + durationTicks;
        CooldownUntilTick = Runner.Tick + cooldownTicks;
    }
 
    public override void FixedUpdateNetwork()
    {
        
        if (HasStateAuthority && ActiveUntilTick != 0 && Runner.Tick >= ActiveUntilTick)
            ActiveUntilTick = 0;
    }
 
    public override void Render()
    {
        bool active = IsActive;
 
        if (active == lastActiveState)
            return;
 
        lastActiveState = active;
        Debug.Log($"[Minefield] {name}: durum değişti -> active={active} (active=true ise GÖRÜNMEZ olmalı)", this);
        SetMineState(active);
    }
 
    
    private void SetMineState(bool active)
    {
        if (mines == null)
            return;
 
        foreach (MineEntry mine in mines)
        {
            if (mine == null || mine.damageObject == null)
                continue;
 
            EnsureCached(mine);
 
            // Visibility only: renderers OFF while active (invisible), ON otherwise.
            if (mine.cachedRenderers != null)
            {
                foreach (Renderer r in mine.cachedRenderers)
                {
                    if (r != null)
                        r.enabled = !active;
                }
 
                // Sadece ilk mayın için teşhis: gerçekten kapandı mı?
                if (mine == mines[0] && mine.cachedRenderers.Length > 0 && mine.cachedRenderers[0] != null)
                    Debug.Log($"[Minefield] {name}: ilk mayın SetMineState(active={active}) sonrası renderer.enabled={mine.cachedRenderers[0].enabled}, cache uzunluk={mine.cachedRenderers.Length}", mine.cachedRenderers[0]);
            }
            else
            {
                if (mine == mines[0])
                    Debug.LogWarning($"[Minefield] {name}: ilk mayının cachedRenderers NULL! renderer bulunamadı.", mine.damageObject);
            }
 
            
            if (mine.cachedColliders != null)
            {
                foreach (Collider c in mine.cachedColliders)
                {
                    if (c != null && !c.enabled)
                        c.enabled = true;
                }
            }
        }
    }
 
    
    private void EnsureCached(MineEntry mine)
    {
        if (mine.cached)
            return;
 
        // Renderers
        if (mine.visuals != null && mine.visuals.Length > 0)
            mine.cachedRenderers = mine.visuals;
        else
            mine.cachedRenderers = mine.damageObject.GetComponentsInChildren<Renderer>(true);
 
        // Trigger colliders
        if (mine.triggerColliders != null && mine.triggerColliders.Length > 0)
        {
            mine.cachedColliders = mine.triggerColliders;
        }
        else
        {
            Collider[] all = mine.damageObject.GetComponentsInChildren<Collider>(true);
            // Keep only trigger colliders (the DamageVolume relies on a trigger).
            int count = 0;
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i].isTrigger) count++;
 
            Collider[] triggers = new Collider[count];
            int idx = 0;
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i].isTrigger) triggers[idx++] = all[i];
 
            mine.cachedColliders = triggers;
        }
 
        mine.cached = true;
    }
 
    private void OnValidate()
    {
        mineActiveDuration = Mathf.Max(0f, mineActiveDuration);
        cooldown = Mathf.Max(0f, cooldown);
    }
}
