using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerDamageFeedback : NetworkBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip damageClip;
    [SerializeField] private AudioClip eliminationClip;
    [SerializeField] private Color damageFlashColor = new Color(1f, 0.15f, 0.1f, 1f);
    [SerializeField] private Color eliminationFlashColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    [SerializeField] private float damageFlashDuration = 0.12f;
    [SerializeField] private float eliminationFlashDuration = 0.35f;

    private int lastHealth = -1;
    private Coroutine flashRoutine;

    private void Awake()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public override void Spawned()
    {
        Bind();
    }

    private void OnEnable()
    {
        if (Object != null && Object.IsValid)
            Bind();
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void Bind()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (playerHealth == null)
            return;

        playerHealth.HealthChanged -= OnHealthChanged;
        playerHealth.Eliminated -= OnEliminated;
        playerHealth.HealthChanged += OnHealthChanged;
        playerHealth.Eliminated += OnEliminated;
        lastHealth = playerHealth.CurrentHealth;
    }

    private void Unbind()
    {
        if (playerHealth == null)
            return;

        playerHealth.HealthChanged -= OnHealthChanged;
        playerHealth.Eliminated -= OnEliminated;
    }

    private void OnHealthChanged(PlayerHealth health, int currentHealth, int maxHealth)
    {
        if (lastHealth >= 0 && currentHealth < lastHealth && currentHealth > 0)
            PlayDamageFeedback();

        lastHealth = currentHealth;
    }

    private void OnEliminated(PlayerHealth health)
    {
        PlayEliminationFeedback();
    }

    private void PlayDamageFeedback()
    {
        PlayClip(damageClip);
        StartFlash(damageFlashColor, damageFlashDuration);
    }

    private void PlayEliminationFeedback()
    {
        PlayClip(eliminationClip);
        StartFlash(eliminationFlashColor, eliminationFlashDuration);
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }

    private void StartFlash(Color color, float duration)
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(Flash(color, duration));
    }

    private IEnumerator Flash(Color color, float duration)
    {
        List<MaterialColorState> states = CollectMaterialStates();

        ApplyColor(states, color);
        yield return new WaitForSeconds(Mathf.Max(0.01f, duration));
        RestoreColor(states);

        flashRoutine = null;
    }

    private List<MaterialColorState> CollectMaterialStates()
    {
        Renderer[] renderers = visualRoot == null
            ? GetComponentsInChildren<Renderer>(true)
            : visualRoot.GetComponentsInChildren<Renderer>(true);

        List<MaterialColorState> states = new List<MaterialColorState>();

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer currentRenderer = renderers[i];

            if (currentRenderer == null)
                continue;

            if (currentRenderer.GetComponentInParent<Canvas>() != null)
                continue;

            Material[] materials = currentRenderer.materials;

            for (int j = 0; j < materials.Length; j++)
            {
                Material material = materials[j];

                if (material == null)
                    continue;

                string propertyName = GetColorPropertyName(material);

                if (string.IsNullOrEmpty(propertyName))
                    continue;

                states.Add(new MaterialColorState(material, propertyName, material.GetColor(propertyName)));
            }
        }

        return states;
    }

    private string GetColorPropertyName(Material material)
    {
        if (material.HasProperty("_BaseColor"))
            return "_BaseColor";

        if (material.HasProperty("_Color"))
            return "_Color";

        return string.Empty;
    }

    private void ApplyColor(List<MaterialColorState> states, Color color)
    {
        for (int i = 0; i < states.Count; i++)
            states[i].Material.SetColor(states[i].PropertyName, color);
    }

    private void RestoreColor(List<MaterialColorState> states)
    {
        for (int i = 0; i < states.Count; i++)
            states[i].Material.SetColor(states[i].PropertyName, states[i].OriginalColor);
    }

    private readonly struct MaterialColorState
    {
        public readonly Material Material;
        public readonly string PropertyName;
        public readonly Color OriginalColor;

        public MaterialColorState(Material material, string propertyName, Color originalColor)
        {
            Material = material;
            PropertyName = propertyName;
            OriginalColor = originalColor;
        }
    }
}
