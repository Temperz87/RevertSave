using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Reflection;
using HarmonyLib;
public class Main : VTOLMOD
{
    private IEnumerator reSubQuicksave()
    {
        while (VTMapManager.fetch == null || !VTMapManager.fetch.scenarioReady || FlightSceneManager.instance.switchingScene)
        {
            yield return null;
        }
        QuicksaveManager.instance.OnQuicksave += OnQuicksave;
    }
        private void OnQuicksave(ConfigNode qsNode)
    {
        Debug.Log("Preforming modified quicksave");
        ConfigNode configNode = new ConfigNode("ModdedSaveFile");
        qsNode.AddNode(configNode);
        CampaignScenario cur_Scenario = PilotSaveManager.currentScenario;
        Campaign cur_Campaign = PilotSaveManager.currentCampaign;
        configNode.SetValue<string>("scenarioID", cur_Scenario.scenarioID);
        /*configNode.SetValue<string>("scenarioName", cur_Scenario.scenarioName);
        configNode.SetValue<string>("scenarioID", cur_Scenario.scenarioID);
        configNode.SetValue<string>("mapID", VTMapManager.fetch.map.mapID);
        configNode.SetValue<string>("scenarioDescription", cur_Scenario.description);*/
        if (cur_Campaign != null)
        {
            configNode.SetValue<string>("campaignID", cur_Campaign.campaignID);
            if (cur_Campaign.isBuiltIn)
            {
                configNode.SetValue<string>("campaignType", "builtIn");
            }
            else if (cur_Campaign.isSteamworksStandalone)
            {
                configNode.SetValue<string>("campaignType", "SSS");
            }
            else if (cur_Campaign.isStandaloneScenarios)
            {
                configNode.SetValue<string>("campaignType", "SS");
            }
            else
            {
                configNode.SetValue<string>("campaignType", "other");
            }
        }
    }
    public void StartLoad()
    {
        Debug.Log("Load Started!");
        StartCoroutine(LoadFromFile());
    }

    private IEnumerator LoadFromFile()
    {
        ConfigNode qsNode = ConfigNode.LoadFromFile(PilotSaveManager.saveDataPath + "/quicksave.cfg");
        ConfigNode config = qsNode.GetNode("ModdedSaveFile");
        foreach (var thing in config.GetNodes())
        {
            Debug.Log(thing + "\n");
        }
        string scenarioID = config.GetValue<String>("scenarioID");
        string campaignID = config.GetValue<String>("campaignID");
        Campaign campaign = new Campaign();
        campaign.campaignID = campaignID;
        switch (config.GetValue<string>("campaignType"))
        {
            case "builtIn":
                campaign.isBuiltIn = true;
                break;
            case "SSS":
                campaign.isSteamworksStandalone = true;
                break;
            case "SS":
                campaign.isSteamworksStandalone = true;
                break;
            // Other is handled by not changing any vars.
        }
        VTScenarioInfo VTSI = VTResources.GetScenario(scenarioID, campaign);
        if (VTSI == null)
        {
            Log(" failed");
        }
        else
        {
            if (VTResources.GetBuiltInCampaigns() != null)
            {
                using (List<VTCampaignInfo>.Enumerator enumerator = VTResources.GetBuiltInCampaigns().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        VTCampaignInfo vtcampaignInfo = enumerator.Current;
                        if (vtcampaignInfo.campaignID == campaignID)
                        {
                            Debug.Log("Setting Campaign");
                            PilotSaveManager.currentCampaign = vtcampaignInfo.ToIngameCampaign();
                            Debug.Log("Setting Vehicle");
                            PilotSaveManager.currentVehicle = VTResources.GetPlayerVehicle(vtcampaignInfo.vehicle);
                            Debug.Log("Set campaign");
                            break;
                        }
                    }
                }
            }
            foreach (CampaignScenario campaignScenario in PilotSaveManager.currentCampaign.missions)
            {
                if (campaignScenario.scenarioID == scenarioID)
                {
                    PilotSaveManager.currentScenario = campaignScenario;
                    Debug.Log("Set scenario");
                    break;
                }
            }
            VTScenario.LaunchScenario(VTSI, false);
            while (VTMapManager.fetch == null || !VTMapManager.fetch.scenarioReady)
            {
                yield return null;
            }
            Debug.Log("Quick Loading");
            QuicksaveManager.instance.Quickload();
        }
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            StartLoad();
        }
        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (QuicksaveManager.instance.CheckQsEligibility() && QuicksaveManager.instance.CheckScenarioQsLimits())
            {
                QuicksaveManager.instance.Quicksave();
            }
        }
    }
    private void Start()
    {
        QuicksaveManager.instance.OnQuicksave += OnQuicksave;
        StartCoroutine(DrawButton());
        ModLoaded();
    }
    private IEnumerator DrawButton()
    {
        // I ripped this from marsh's code soooooooooooooooooooo
        Log("Ripping off multiplayer");
        Transform ScenarioDisplay = GameObject.Find("InteractableCanvas").transform.GetChild(0).GetChild(6).GetChild(0).GetChild(1);
        if (ScenarioDisplay.name != "ScenarioDisplay")
        {
            Log($"ScenarioDisplay was wrong ({ScenarioDisplay.name}), trying other method");
            ScenarioDisplay = GameObject.Find("InteractableCanvas").transform.GetChild(0).GetChild(8).GetChild(0).GetChild(1);
            Log($"ScenarioDisplay now == {ScenarioDisplay.name}");
        }
        Transform mpButton = Instantiate(ScenarioDisplay.GetChild(9).gameObject, ScenarioDisplay).transform;
        Log("Multiplayer Button" + mpButton.name);
        mpButton.gameObject.SetActive(true);
        mpButton.name = "MPButton";
        mpButton.GetComponent<RectTransform>().localPosition = new Vector3(601, -325);
        mpButton.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 206.7f);
        mpButton.GetComponentInChildren<Text>().text = "QL";
        mpButton.GetComponent<Image>().color = Color.cyan;
        mpButton.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
        VRInteractable mpInteractable = mpButton.GetComponent<VRInteractable>();
        mpInteractable.interactableName = "QuickLoad";
        UnityAction UA = new UnityAction(StartLoad);
        mpInteractable.OnInteract = new UnityEvent();
        mpInteractable.OnInteract.AddListener(UA);
        yield return null;
    }
    public override void ModLoaded()
    {
        VTOLAPI.SceneLoaded += lol;
        VTOLAPI.MissionReloaded += () =>
        {
            StartCoroutine(reSubQuicksave());
        };
        base.ModLoaded();
    }
    private void lol(VTOLScenes scenes)
    {
        if (scenes == VTOLScenes.Akutan || scenes == VTOLScenes.CustomMapBase) // Don't forget to add running bool
        {
            QuicksaveManager.instance.OnQuicksave += OnQuicksave;
        }
        if (scenes == VTOLScenes.ReadyRoom)
        {
            StartCoroutine(DrawButton());
        }
    }
}