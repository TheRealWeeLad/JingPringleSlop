using UnityEngine;
using UnityEngine.UI;

public class UIManagerFluid : MonoBehaviour
{
    public ParticleManager particleManager;
    public ParticleManagerNew newParticleManager;
    public GameObject menuObj;
    public GameObject oldSettingsObj;
    public GameObject newSettingsObj;

    [Space()]
    [Header("Slider Objects")]
    public Slider distanceSlider;
    public Slider stabilitySlider;
    public Slider steepnessSlider;
    public Slider frictionSlider;
    public Slider massSlider;

    void Awake()
    {
        GameObject particleManagerObj = GameObject.Find("Particle Manager");
        particleManager = particleManagerObj.GetComponent<ParticleManager>();
        particleManager.enabled = false;
        newParticleManager = particleManagerObj.GetComponent<ParticleManagerNew>();
        newParticleManager.enabled = false;

        menuObj = GameObject.Find("Menu");
        oldSettingsObj = GameObject.Find("Old Settings");
        oldSettingsObj.SetActive(false);
        newSettingsObj = GameObject.Find("New Settings");
        newSettingsObj.SetActive(false);
    }

    void HideMenu() => menuObj.SetActive(false);
    void ShowMenu() => menuObj.SetActive(true);
    public void ActivateOldSimulation()
    {
        HideMenu();
        particleManager.enabled = true;
        oldSettingsObj.SetActive(true);
    }
    public void ActivateNewSimulation()
    {
        HideMenu();
        newParticleManager.enabled = true;
        newSettingsObj.SetActive(true);
    }
    public void ReturnFromOld()
    {
        ShowMenu();
        particleManager.enabled = false;
        oldSettingsObj.SetActive(false);
    }
    public void ReturnFromNew()
    {
        ShowMenu();
        newParticleManager.enabled = false;
        newSettingsObj.SetActive(false);
    }

    public void Quit() => GameManager.QuitGame();

    // Slider Functions
    public void ChangeDistance() => particleManager.mostEffectiveDistance = distanceSlider.value;
    public void ChangeStability() => particleManager.stability = stabilitySlider.value;
    public void ChangeSteepness() => particleManager.steepness = steepnessSlider.value;
    public void ChangeFriction() => particleManager.frictionStrength = frictionSlider.value;
    public void ChangeMass() => particleManager.mass = massSlider.value;
}
