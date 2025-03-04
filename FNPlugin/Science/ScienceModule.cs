﻿using FNPlugin.Constants;
using FNPlugin.Refinery;
using System;
using System.Linq;
using FNPlugin.Powermanagement;
using FNPlugin.Refinery.Activity;
using FNPlugin.Resources;
using UnityEngine;
using KSP.Localization;

namespace FNPlugin
{
    class ScienceModule : ResourceSuppliableModule, ITelescopeController, IUpgradeableModule
    {
        // persistant true
        [KSPField(isPersistant = true)] public bool IsEnabled;
        [KSPField(isPersistant = true)] public int active_mode = 0;
        [KSPField(isPersistant = true)] public double last_active_time;
        [KSPField(isPersistant = true)] public double electrical_power_ratio;
        [KSPField(isPersistant = true)] public bool isupgraded;

        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_ScienceModule_Activity")]//Activity
        public string statusTitle;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_ScienceModule_Power")]//Power
        public string powerStr;

        [KSPField(guiActive = true, guiName = "R")] public string reprocessingRate;
        [KSPField(guiActive = true, guiName = "A")] public string antimatterRate;
        [KSPField(guiActive = true, guiName = "E")] public string electrolysisRate;
        [KSPField(guiActive = true, guiName = "C")] public string centrifugeRate;

        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_ScienceModule_AntimatterEfficiency")]//Antimatter Efficiency
        public string antimatterProductionEfficiency;

        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_ScienceModule_DataProcessingMultiplier")] //Data Processing Multiplier
        public float dataProcessingMultiplier;

        [KSPField] public string Mk2Tech = "longTermScienceTech";
        [KSPField] public string Mk3Tech = "scientificOutposts";
        [KSPField] public string Mk4Tech = "highEnergyScience";
        [KSPField] public string Mk5Tech = "appliedHighEnergyPhysics";
        [KSPField] public string Mk6Tech = "ultraHighEnergyPhysics";
        [KSPField] public string Mk7Tech = "extremeHighEnergyPhysics";

        [KSPField] public int Mk1ScienceCap = 1000;
        [KSPField] public int Mk2ScienceCap = 1600;
        [KSPField] public int Mk3ScienceCap = 2500;
        [KSPField] public int Mk4ScienceCap = 4000;
        [KSPField] public int Mk5ScienceCap = 6350;
        [KSPField] public int Mk6ScienceCap = 10000;
        [KSPField] public int Mk7ScienceCap = 16000;

        // persistent false
        [KSPField] public string animName1 = "";
        [KSPField] public string animName2 = "";
        [KSPField] public string upgradeTechReq = null;
        [KSPField] public float upgradeCost = 20;

        [KSPField] public float powerReqMult = 1;
        [KSPField] public float baseDataStorage = 750;

        protected int techLevel;

        protected string[] modes = { "Scanning", "Reprocessing", "Producing Antimatter", "Electrolysing", "Centrifuging" };

        protected double science_rate_f;
        protected double reprocessing_rate_f;
        protected float crew_capacity_ratio;
        protected double antimatter_rate_f;

        protected float electrolysis_rate_f;
        protected double deut_rate_f;
        protected bool play_down = true;
        protected bool hasrequiredupgrade;

        protected Animation anim;
        protected Animation anim2;
        protected NuclearFuelReprocessor reprocessor;
        protected AntimatterGenerator antimatterGenerator;
        protected ModuleScienceConverter moduleScienceConverter;
        protected ModuleScienceLab moduleScienceLab;

        public bool CanProvideTelescopeControl => part.protoModuleCrew.Count > 0;

        /*
        [KSPEvent(guiActive = true, guiName = "Begin Scanning", active = true)]
        public void BeginResearch()
        {
            if (crew_capacity_ratio == 0) return;

            IsEnabled = true;
            active_mode = 0;

            anim[animName1].speed = 1;
            anim[animName1].normalizedTime = 0;
            anim.Blend(animName1, 2);
            anim2[animName2].speed = 1;
            anim2[animName2].normalizedTime = 0;
            anim2.Blend(animName2, 2);
            play_down = true;
        }
        */

        private void DetermineTechLevel()
        {
            techLevel = 1;
            if (PluginHelper.UpgradeAvailable(Mk2Tech))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk3Tech))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk4Tech))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk5Tech))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk6Tech))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk7Tech))
                techLevel++;
        }

        private int GetScienceCap()
        {
            if (techLevel == 1)
                return Mk1ScienceCap;
            if (techLevel == 2)
                return Mk2ScienceCap;
            if (techLevel == 3)
                return Mk3ScienceCap;
            if (techLevel == 4)
                return Mk4ScienceCap;
            if (techLevel == 5)
                return Mk5ScienceCap;
            if (techLevel == 6)
                return Mk6ScienceCap;
            if (techLevel == 7)
                return Mk7ScienceCap;
            return 0;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_ScienceModule_ReprocessNuclearFuel", active = true)]//Reprocess Nuclear Fuel
        public void ReprocessFuel()
        {
            if (crew_capacity_ratio == 0) return;

            IsEnabled = true;
            active_mode = 1;

            anim[animName1].speed = 1;
            anim[animName1].normalizedTime = 0;
            anim.Blend(animName1, 2);
            anim2[animName2].speed = 1;
            anim2[animName2].normalizedTime = 0;
            anim2.Blend(animName2, 2);
            play_down = true;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_ScienceModule_ActivateAntimatterProduction", active = true)]//Activate Antimatter Production
        public void ActivateFactory()
        {
            if (crew_capacity_ratio == 0) return;

            IsEnabled = true;
            active_mode = 2;

            anim[animName1].speed = 1;
            anim[animName1].normalizedTime = 0;
            anim.Blend(animName1, 2);
            anim2[animName2].speed = 1;
            anim2[animName2].normalizedTime = 0;
            anim2.Blend(animName2, 2);
            play_down = true;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_ScienceModule_ActivateElectrolysis", active = true)]//Activate Electrolysis
        public void ActivateElectrolysis()
        {
            if (crew_capacity_ratio == 0) return;

            IsEnabled = true;
            active_mode = 3;

            anim[animName1].speed = 1;
            anim[animName1].normalizedTime = 0;
            anim.Blend(animName1, 2);
            anim2[animName2].speed = 1;
            anim2[animName2].normalizedTime = 0;
            anim2.Blend(animName2, 2);
            play_down = true;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_ScienceModule_ActivateCentrifuge", active = true)]//Activate Centrifuge
        public void ActivateCentrifuge()
        {
            if (crew_capacity_ratio == 0) return;

            IsEnabled = true;
            active_mode = 4;

            anim[animName1].speed = 1;
            anim[animName1].normalizedTime = 0;
            anim.Blend(animName1, 2);
            anim2[animName2].speed = 1;
            anim2[animName2].normalizedTime = 0;
            anim2.Blend(animName2, 2);
            play_down = true;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_ScienceModule_StopCurrentActivity", active = false)]//Stop Current Activity
        public void StopActivity()
        {
            IsEnabled = false;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_ScienceModule_Retrofit", active = true)]//Retrofit
        public void RetrofitEngine()
        {
            if (ResearchAndDevelopment.Instance == null || isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) return;

            upgradePartModule();
            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
        }

        public string UpgradeTechnology => upgradeTechReq;

        public void upgradePartModule()
        {
            //canDeploy = true;
            isupgraded = true;
        }

        public override void OnStart(StartState state)
        {
            moduleScienceLab = part.FindModuleImplementing<ModuleScienceLab>();
            moduleScienceConverter = part.FindModuleImplementing<ModuleScienceConverter>();

            if (moduleScienceConverter != null && moduleScienceLab != null)
            {
                DetermineTechLevel();

                var scienceCap = GetScienceCap();
                moduleScienceLab.dataStorage = scienceCap;
                moduleScienceConverter.scienceCap = scienceCap;

                var deminishingScienceModifier = moduleScienceLab.dataStored >= baseDataStorage ? 1 : moduleScienceLab.dataStored / baseDataStorage;

                dataProcessingMultiplier = moduleScienceLab.dataStored < float.Epsilon ? 0.5f
                    : deminishingScienceModifier * (baseDataStorage / moduleScienceLab.dataStorage) * (moduleScienceLab.dataStorage / moduleScienceLab.dataStored) * 0.5f;
                moduleScienceConverter.dataProcessingMultiplier = dataProcessingMultiplier;
            }

            if (state == StartState.Editor)
            {
                if (this.HasTechsRequiredToUpgrade())
                {
                    isupgraded = true;
                    upgradePartModule();
                }
                return;
            }

            if (isupgraded)
                upgradePartModule();
            else
            {
                if (this.HasTechsRequiredToUpgrade())
                    hasrequiredupgrade = true;
            }

            // update gui names
            /*
            Events["BeginResearch"].guiName = beginResearchName;
            */

            reprocessor = new NuclearFuelReprocessor();
            reprocessor.Initialize(part, null);
            antimatterGenerator = new AntimatterGenerator(part, 1, PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.AntiProtium));

            Debug.Log("[KSPI]: ScienceModule on " + part.name + " was Force Activated");
            part.force_activate();

            anim = part.FindModelAnimators(animName1).FirstOrDefault();
            anim2 = part.FindModelAnimators(animName2).FirstOrDefault();
            if (anim != null && anim2 != null)
            {
                if (IsEnabled)
                {
                    //anim [animName1].normalizedTime = 1f;
                    //anim2 [animName2].normalizedTime = 1f;
                    //anim [animName1].speed = -1f;
                    //anim2 [animName2].speed = -1f;
                    anim.Blend(animName1, 1, 0);
                    anim2.Blend(animName2, 1, 0);
                }
                else
                {
                    //anim [animName1].normalizedTime = 0f;
                    //anim2 [animName2].normalizedTime = 0f;
                    //anim [animName1].speed = 1f;
                    //anim2 [animName2].speed = 1f;
                    //anim.Blend (animName1, 0, 0);global_rate_multipliers
                    //anim2.Blend (animName2, 0, 0);
                    play_down = false;
                }
                //anim.Play ();
                //anim2.Play ();
            }

            if (IsEnabled && last_active_time != 0)
            {
                double globalRateMultipliers = 1;
                crew_capacity_ratio = part.protoModuleCrew.Count / (float)part.CrewCapacity;
                globalRateMultipliers = globalRateMultipliers * crew_capacity_ratio;

                /*
                if (active_mode == 0) // Science persistence
                {
                    var time_diff = Planetarium.GetUniversalTime() - last_active_time;
                    var altitude_multiplier = Math.Max((vessel.altitude / (vessel.mainBody.Radius)), 1);
                    var kerbalResearchSkillFactor = part.protoModuleCrew.Sum(proto_crew_member => GetKerbalScienceFactor(proto_crew_member) / 2f);

                    double science_to_increment = kerbalResearchSkillFactor * GameConstants.baseScienceRate * time_diff
                        / PluginHelper.SecondsInDay * electrical_power_ratio * global_rate_multipliers * PluginHelper.getScienceMultiplier(vessel)
                        / (Math.Sqrt(altitude_multiplier));

                    science_to_increment = (double.IsNaN(science_to_increment) || double.IsInfinity(science_to_increment)) ? 0 : science_to_increment;
                    science_to_add += science_to_increment;

                }
                else
                */
                if (active_mode == 2) // Antimatter persistence
                {
                    var deltaTime = Planetarium.GetUniversalTime() - last_active_time;

                    var electricalPowerProvidedInMegajoules = electrical_power_ratio * globalRateMultipliers * powerReqMult * PluginSettings.Config.BaseAMFPowerConsumption * deltaTime;

                    antimatterGenerator.Produce(electricalPowerProvidedInMegajoules);
                }
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            try
            {
                Events["ReprocessFuel"].active = !IsEnabled;
                Events["ActivateFactory"].active = isupgraded && !IsEnabled;
                Events["ActivateElectrolysis"].active = false;
                Events["ActivateCentrifuge"].active = isupgraded && !IsEnabled && vessel.Splashed;
                Events["StopActivity"].active = isupgraded && IsEnabled;
                Fields["statusTitle"].guiActive = isupgraded;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: ScienceModule OnUpdate 1 " + e.Message);
            }

            try
            {
                // only show retrofit if we can actualy upgrade
                Events["RetrofitEngine"].active = ResearchAndDevelopment.Instance == null ? false : !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: ScienceModule OnUpdate 2 " + e.Message);
            }

            if (IsEnabled)
            {
                try
                {
                    //anim [animName1].normalizedTime = 1f;
                    statusTitle = modes[active_mode] + "...";
                }
                catch (Exception e)
                {
                    Debug.LogError("[KSPI]: ScienceModule OnUpdate 3 " + e.Message);
                }

                try
                {
                    /*
                    Fields["scienceRate"].guiActive = false;
                    Fields["collectedScience"].guiActive = false;
                    */
                    Fields["reprocessingRate"].guiActive = false;
                    Fields["antimatterRate"].guiActive = false;
                    Fields["electrolysisRate"].guiActive = false;
                    Fields["centrifugeRate"].guiActive = false;
                    Fields["antimatterProductionEfficiency"].guiActive = false;
                    Fields["powerStr"].guiActive = true;
                }
                catch (Exception e)
                {
                    Debug.LogError("[KSPI]: ScienceModule 4 " + e.Message);
                }

                double currentpowertmp = electrical_power_ratio * PluginSettings.Config.BasePowerConsumption * powerReqMult;

                try
                {
                    powerStr = currentpowertmp.ToString("0.0000") + "MW / " + (powerReqMult * PluginSettings.Config.BasePowerConsumption).ToString("0.0000") + "MW";
                }
                catch (Exception e)
                {
                    Debug.LogError("[KSPI]: ScienceModule 5 " + e.Message);
                }

                /*
                if (active_mode == 0) // Research
                {
                    //Fields["scienceRate"].guiActive = true;
                    //Fields["collectedScience"].guiActive = true;
                    double scienceratetmp = science_rate_f * PluginHelper.SecondsInDay * PluginHelper.getScienceMultiplier(vessel);
                    scienceRate = scienceratetmp.ToString("0.0000") + "/Day";
                    collectedScience = science_to_add.ToString("0.000000");
                }
                else
                */
                if (active_mode == 1) // Fuel Reprocessing
                {
                    try
                    {
                        Fields["reprocessingRate"].guiActive = true;
                        reprocessingRate = Localizer.Format("#LOC_KSPIE_ScienceModule_Reprocessing", reprocessing_rate_f.ToString("0.0"));// + " Hours Remaining"
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[KSPI]: ScienceModule Fuel Reprocessing " + e.Message);
                    }
                }
                else if (active_mode == 2) // Antimatter
                {
                    try
                    {
                        currentpowertmp = electrical_power_ratio * PluginSettings.Config.BaseAMFPowerConsumption * powerReqMult;
                        Fields["antimatterRate"].guiActive = true;
                        Fields["antimatterProductionEfficiency"].guiActive = true;
                        powerStr = currentpowertmp.ToString("0.00") + "MW / " + (powerReqMult * PluginSettings.Config.BaseAMFPowerConsumption).ToString("0.00") + "MW";
                        antimatterProductionEfficiency = (antimatterGenerator.Efficiency * 100).ToString("0.0000") + "%";
                        double antimatterRatePerDay = antimatter_rate_f * PluginSettings.Config.SecondsInDay;

                        if (antimatterRatePerDay > 0.1)
                            antimatterRate = (antimatterRatePerDay).ToString("0.0000") + " mg/day";
                        else
                        {
                            if (antimatterRatePerDay > 0.1e-3)
                                antimatterRate = (antimatterRatePerDay * 1e3).ToString("0.0000") + " ug/day";
                            else
                                antimatterRate = (antimatterRatePerDay * 1e6).ToString("0.0000") + " ng/day";
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[KSPI]: ScienceModule Antimatter " + e.Message);
                    }
                }
                else if (active_mode == 3) // Electrolysis
                {
                    currentpowertmp = electrical_power_ratio * PluginSettings.Config.BaseELCPowerConsumption * powerReqMult;
                    Fields["electrolysisRate"].guiActive = true;
                    double electrolysisratetmp = -electrolysis_rate_f * PluginSettings.Config.SecondsInDay;
                    electrolysisRate = electrolysisratetmp.ToString("0.0") + "mT/day";
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + (powerReqMult * PluginSettings.Config.BaseELCPowerConsumption).ToString("0.00") + "MW";
                }
                else if (active_mode == 4) // Centrifuge
                {
                    currentpowertmp = electrical_power_ratio * PluginSettings.Config.BaseCentriPowerConsumption * powerReqMult;
                    Fields["centrifugeRate"].guiActive = true;
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + (powerReqMult * PluginSettings.Config.BaseCentriPowerConsumption).ToString("0.00") + "MW";
                    double deutPerHour = deut_rate_f * 3600;
                    centrifugeRate = Localizer.Format("#LOC_KSPIE_ScienceModule_Centrifuge", deutPerHour.ToString("0.00"));// + " Kg Deuterium/Hour"
                }
                else
                {
                    // nothing
                }
            }
            else
            {
                if (play_down)
                {
                    if (anim != null)
                    {
                        anim[animName1].speed = -1;
                        anim[animName1].normalizedTime = 1;
                        anim.Blend(animName1, 2);
                    }
                    if (anim2 != null)
                    {
                        anim2[animName2].speed = -1;
                        anim2[animName2].normalizedTime = 1;
                        anim2.Blend(animName2, 2);
                    }
                    play_down = false;
                }

                //anim [animName1].normalizedTime = 0;


                try
                {
                    /*
                    Fields["scienceRate"].guiActive = false;
                    Fields["collectedScience"].guiActive = false;
                    */


                    Fields["reprocessingRate"].guiActive = false;
                    Fields["antimatterRate"].guiActive = false;
                    Fields["powerStr"].guiActive = false;
                    Fields["centrifugeRate"].guiActive = false;
                    Fields["electrolysisRate"].guiActive = false;
                    Fields["antimatterProductionEfficiency"].guiActive = false;
                }
                catch (Exception e)
                {
                    Debug.LogError("[KSPI]: OnUpdate Else " + e.Message);
                }

                if (crew_capacity_ratio > 0)
                    statusTitle = Localizer.Format("#LOC_KSPIE_ScienceModule_Idle");//"Idle"
                else
                    statusTitle = Localizer.Format("#LOC_KSPIE_ScienceModule_Notenoughcrew");//"Not enough crew"
            }
        }

        private float GetKerbalScienceFactor(ProtoCrewMember kerbal)
        {
            float kerbalFactor;

            // initialise with profession
            if (kerbal.experienceTrait.Title == "Scientist")
                kerbalFactor = 1.5f;
            else if (kerbal.experienceTrait.Title == "Engineer")
                kerbalFactor = 1f;
            else
                kerbalFactor = 0.5f;

            // moidy by experience level
            kerbalFactor *= (kerbal.experienceLevel + 10f) / 10f;

            // final modify result by kerbal stupidity (+/- 10%)
            return kerbalFactor * (1.1f - (kerbal.stupidity / 5f));
        }

        public override void OnFixedUpdate()
        {
            double globalRateMultipliers = 1;
            crew_capacity_ratio = ((float)part.protoModuleCrew.Count) / ((float)part.CrewCapacity);
            globalRateMultipliers = globalRateMultipliers * crew_capacity_ratio;

            if (!IsEnabled) return;

            /*
            if (active_mode == 0)  // Research
            {
                var powerRequest = powerReqMult * PluginHelper.BasePowerConsumption * TimeWarp.fixedDeltaTime;

                double electrical_power_provided = CheatOptions.InfiniteElectricity
                    ? powerRequest
                    : consumeFNResource(powerRequest, ResourceManager.FNRESOURCE_MEGAJOULES);

                electrical_power_ratio = electrical_power_provided / TimeWarp.fixedDeltaTime / PluginHelper.BasePowerConsumption / powerReqMult;
                global_rate_multipliers = global_rate_multipliers * electrical_power_ratio;

                double kerbalScienceSkillFactor = part.protoModuleCrew.Sum(proto_crew_member => GetKerbalScienceFactor(proto_crew_member) / 2f);
                double altitude_multiplier = Math.Max(vessel.altitude / (vessel.mainBody.Radius), 1);

                science_rate_f = (kerbalScienceSkillFactor * GameConstants.baseScienceRate * PluginHelper.getScienceMultiplier(vessel)
                    / PluginHelper.SecondsInDay * global_rate_multipliers
                    / (Math.Sqrt(altitude_multiplier)));

                if (ResearchAndDevelopment.Instance != null && !double.IsNaN(science_rate_f) && !double.IsInfinity(science_rate_f))
                {
                    science_to_add += science_rate_f * TimeWarp.fixedDeltaTime;
                }
            }
            else
            */
            if (active_mode == 1) // Fuel Reprocessing
            {
                var powerRequest = powerReqMult * PluginSettings.Config.BasePowerConsumption * TimeWarp.fixedDeltaTime;

                double electricalPowerProvided = CheatOptions.InfiniteElectricity
                    ? powerRequest
                    : consumeFNResource(powerRequest, ResourceSettings.Config.ElectricPowerInMegawatt);

                electrical_power_ratio = electricalPowerProvided / TimeWarp.fixedDeltaTime / PluginSettings.Config.BasePowerConsumption / powerReqMult;

                var productionModifier = globalRateMultipliers;
                globalRateMultipliers = globalRateMultipliers * electrical_power_ratio;
                reprocessor.UpdateFrame(globalRateMultipliers, electrical_power_ratio, productionModifier, true, TimeWarp.fixedDeltaTime);

                if (reprocessor.GetActinidesRemovedPerHour() > 0)
                    reprocessing_rate_f = reprocessor.GetRemainingAmountToReprocess() / reprocessor.GetActinidesRemovedPerHour();
                else
                    IsEnabled = false;
            }
            else if (active_mode == 2) //Antimatter
            {
                var powerRequestInMegajoules = powerReqMult * PluginSettings.Config.BaseAMFPowerConsumption * TimeWarp.fixedDeltaTime;

                var energyProvidedInMegajoules = CheatOptions.InfiniteElectricity
                    ? powerRequestInMegajoules
                    : consumeFNResource(powerRequestInMegajoules, ResourceSettings.Config.ElectricPowerInMegawatt);

                electrical_power_ratio = powerRequestInMegajoules > 0 ? energyProvidedInMegajoules / powerRequestInMegajoules : 0;
                antimatterGenerator.Produce(energyProvidedInMegajoules * globalRateMultipliers);
                antimatter_rate_f = antimatterGenerator.ProductionRate;
            }
            else if (active_mode == 3)
            {
                IsEnabled = false;
            }
            else if (active_mode == 4) // Centrifuge
            {
                if (vessel.Splashed)
                {
                    var powerRequest = powerReqMult * PluginSettings.Config.BaseCentriPowerConsumption * TimeWarp.fixedDeltaTime;

                    double electricalPowerProvided = CheatOptions.InfiniteElectricity
                        ? powerRequest
                        : consumeFNResource(powerRequest, ResourceSettings.Config.ElectricPowerInMegawatt);

                    electrical_power_ratio = electricalPowerProvided / TimeWarp.fixedDeltaTime / PluginSettings.Config.BaseCentriPowerConsumption / powerReqMult;
                    globalRateMultipliers = globalRateMultipliers * electrical_power_ratio;
                    double deutProduced = globalRateMultipliers * GameConstants.deuterium_timescale * GameConstants.deuterium_abudance * 1000.0f;
                    deut_rate_f = -part.RequestResource(ResourceSettings.Config.DeuteriumLqd, -deutProduced * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime;
                }
                else
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_ScienceModule_Postmsg"), 5.0f, ScreenMessageStyle.UPPER_CENTER);//"You must be splashed down to perform this activity."
                    IsEnabled = false;
                }
            }

            if (electrical_power_ratio <= 0)
            {
                deut_rate_f = 0;
                electrolysis_rate_f = 0;
                science_rate_f = 0;
                antimatter_rate_f = 0;
                reprocessing_rate_f = 0;
            }

            last_active_time = Planetarium.GetUniversalTime();
        }

        /*
        protected override bool generateScienceData()
        {
            ScienceExperiment experiment = ResearchAndDevelopment.GetExperiment(experimentID);
            if (experiment == null) return false;

            if (science_to_add > 0)
            {
                ScienceSubject subject = ResearchAndDevelopment.GetExperimentSubject(experiment, ScienceUtil.GetExperimentSituation(vessel), vessel.mainBody, "", "");
                if (subject == null)
                    return false;
                subject.subjectValue = PluginHelper.getScienceMultiplier(vessel);
                subject.scienceCap = 167 * subject.subjectValue; ///PluginHelper.getScienceMultiplier(vessel.mainBody.flightGlobalsIndex, false);
                subject.dataScale = 1.25f;

                float remaining_base_science = (subject.scienceCap - subject.science) / subject.subjectValue;
                science_to_add = Math.Min(science_to_add, remaining_base_science);

                // transmission of zero data breaks the experiment result dialog box
                data_size = Math.Max(float.Epsilon, science_to_add * subject.dataScale);
                science_data = new ScienceData((float)data_size, 1, 0, subject.id, "Science Lab Data");

                result_title = experiment.experimentTitle;
                result_string = "Science experiments were conducted in the vicinity of " + vessel.mainBody.name + ".";

                recovery_value = science_to_add;
                transmit_value = recovery_value;
                xmit_scalar = 1;

                ref_value = subject.scienceCap;

                return true;
            }
            return false;
        }

        protected override void cleanUpScienceData()
        {
            science_to_add = 0;
        }
        */

        public override string getResourceManagerDisplayName()
        {
            if (IsEnabled)
                return Localizer.Format("#LOC_KSPIE_ScienceModule_ResourceManagerDisplayName", modes[active_mode]);//"Science Lab (" +  + ")"

            return Localizer.Format("#LOC_KSPIE_ScienceModule_ResourceManagerDisplayName2");//"Science Lab"
        }

    }
}
