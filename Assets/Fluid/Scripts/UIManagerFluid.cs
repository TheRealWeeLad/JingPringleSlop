using UnityEngine;
using UnityEngine.UI;

public class UIManagerFluid : MonoBehaviour
{
    public GameObject particleManager;
    public GameObject menuObj;
    public GameObject oldSettingsObj;

    ParticleManager _oldParticleManager;

    [Space()]
    [Header("Slider Objects")]
    public Slider distanceSlider;
    public Slider stabilitySlider;
    public Slider steepnessSlider;
    public Slider frictionSlider;
    public Slider massSlider;

    void Awake()
    {
        particleManager = GameObject.Find("Particle Manager");
        _oldParticleManager = particleManager.GetComponent<ParticleManager>();
        _oldParticleManager.enabled = false;

        menuObj = GameObject.Find("Menu");
        oldSettingsObj = GameObject.Find("Old Settings");
        oldSettingsObj.SetActive(false);
    }

    void HideMenu()
    {
        menuObj.SetActive(false);
    }
    public void ActivateOldSimulation()
    {
        HideMenu();
        _oldParticleManager.enabled = true;
        oldSettingsObj.SetActive(true);
    }

    public void Quit() => GameManager.QuitGame();

    // Slider Functions
    public void ChangeDistance() => _oldParticleManager.mostEffectiveDistance = distanceSlider.value;
    public void ChangeStability() => _oldParticleManager.stability = stabilitySlider.value;
    public void ChangeSteepness() => _oldParticleManager.steepness = steepnessSlider.value;
    public void ChangeFriction() => _oldParticleManager.frictionStrength = frictionSlider.value;
    public void ChangeMass() => _oldParticleManager.mass = massSlider.value;
}
