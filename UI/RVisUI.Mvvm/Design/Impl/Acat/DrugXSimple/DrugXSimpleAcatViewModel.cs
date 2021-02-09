using LanguageExt;
using RVis.Model;
using System;
using UnitsNet;

namespace RVisUI.Mvvm.Design
{
  public sealed class DrugXSimpleAcatViewModel : IDrugXSimpleAcatViewModel
  {
    public MolarMass? MMN { get => MolarMass.FromGramsPerMole(123); set => throw new NotImplementedException(); }
    public string? MMT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsMMPreset { get => true; set => throw new NotImplementedException(); }
    public Duration? TlagN { get => Duration.FromHours(123); set => throw new NotImplementedException(); }
    public string? TlagT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsTlagPreset { get => true; set => throw new NotImplementedException(); }
    public double? f_Abs_stomN { get => 123; set => throw new NotImplementedException(); }
    public string? f_Abs_stomT { get => "456"; set => throw new NotImplementedException(); }
    public bool Isf_Abs_stomPreset { get => true; set => throw new NotImplementedException(); }
    public double? f_Abs_duodN { get => 0; set => throw new NotImplementedException(); }
    public string? f_Abs_duodT { get => "456"; set => throw new NotImplementedException(); }
    public bool Isf_Abs_duodPreset { get => true; set => throw new NotImplementedException(); }
    public double? f_Abs_jejuN { get => 123; set => throw new NotImplementedException(); }
    public string? f_Abs_jejuT { get => "456"; set => throw new NotImplementedException(); }
    public bool Isf_Abs_jejuPreset { get => true; set => throw new NotImplementedException(); }
    public double? f_Abs_ileonN { get => 0; set => throw new NotImplementedException(); }
    public string? f_Abs_ileonT { get => "456"; set => throw new NotImplementedException(); }
    public bool Isf_Abs_ileonPreset { get => true; set => throw new NotImplementedException(); }
    public double? f_Abs_cecumN { get => 123; set => throw new NotImplementedException(); }
    public string? f_Abs_cecumT { get => "456"; set => throw new NotImplementedException(); }
    public bool Isf_Abs_cecumPreset { get => true; set => throw new NotImplementedException(); }
    public double? f_Abs_colonN { get => 0; set => throw new NotImplementedException(); }
    public string? f_Abs_colonT { get => "456"; set => throw new NotImplementedException(); }
    public bool Isf_Abs_colonPreset { get => true; set => throw new NotImplementedException(); }
    public double? PeffN { get => 123; set => throw new NotImplementedException(); }
    public string? PeffT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsPeffPreset { get => true; set => throw new NotImplementedException(); }
    public double? Ratio_BPN { get => 123; set => throw new NotImplementedException(); }
    public string? Ratio_BPT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsRatio_BPPreset { get => true; set => throw new NotImplementedException(); }
    public double? PC_adipN { get => 123; set => throw new NotImplementedException(); }
    public string? PC_adipT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsPC_adipPreset { get => true; set => throw new NotImplementedException(); }
    public double? PC_kidN { get => 123; set => throw new NotImplementedException(); }
    public string? PC_kidT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsPC_kidPreset { get => true; set => throw new NotImplementedException(); }
    public double? PC_rptN { get => 123; set => throw new NotImplementedException(); }
    public string? PC_rptT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsPC_rptPreset { get => true; set => throw new NotImplementedException(); }
    public double? PC_pptN { get => 123; set => throw new NotImplementedException(); }
    public string? PC_pptT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsPC_pptPreset { get => true; set => throw new NotImplementedException(); }
    public double? PC_liverN { get => 123; set => throw new NotImplementedException(); }
    public string? PC_liverT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsPC_liverPreset { get => true; set => throw new NotImplementedException(); }
    public double? PC_stomN { get => 123; set => throw new NotImplementedException(); }
    public string? PC_stomT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsPC_stomPreset { get => true; set => throw new NotImplementedException(); }
    public double? PC_duodN { get => 123; set => throw new NotImplementedException(); }
    public string? PC_duodT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsPC_duodPreset { get => true; set => throw new NotImplementedException(); }
    public double? PC_jejuN { get => 123; set => throw new NotImplementedException(); }
    public string? PC_jejuT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsPC_jejuPreset { get => true; set => throw new NotImplementedException(); }
    public double? PC_ileonN { get => 123; set => throw new NotImplementedException(); }
    public string? PC_ileonT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsPC_ileonPreset { get => true; set => throw new NotImplementedException(); }
    public double? PC_cecumN { get => 123; set => throw new NotImplementedException(); }
    public string? PC_cecumT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsPC_cecumPreset { get => true; set => throw new NotImplementedException(); }
    public double? PC_colonN { get => 123; set => throw new NotImplementedException(); }
    public string? PC_colonT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsPC_colonPreset { get => true; set => throw new NotImplementedException(); }
    public double? Fu_adipN { get => 123; set => throw new NotImplementedException(); }
    public string? Fu_adipT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsFu_adipPreset { get => true; set => throw new NotImplementedException(); }
    public double? Fu_kidN { get => 123; set => throw new NotImplementedException(); }
    public string? Fu_kidT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsFu_kidPreset { get => true; set => throw new NotImplementedException(); }
    public double? Fu_pptN { get => 123; set => throw new NotImplementedException(); }
    public string? Fu_pptT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsFu_pptPreset { get => true; set => throw new NotImplementedException(); }
    public double? Fu_rptN { get => 123; set => throw new NotImplementedException(); }
    public string? Fu_rptT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsFu_rptPreset { get => true; set => throw new NotImplementedException(); }
    public double? Fu_bloodN { get => 123; set => throw new NotImplementedException(); }
    public string? Fu_bloodT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsFu_bloodPreset { get => true; set => throw new NotImplementedException(); }
    public double? Fu_liverN { get => 123; set => throw new NotImplementedException(); }
    public string? Fu_liverT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsFu_liverPreset { get => true; set => throw new NotImplementedException(); }
    public double? Fu_portveinN { get => 123; set => throw new NotImplementedException(); }
    public string? Fu_portveinT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsFu_portveinPreset { get => true; set => throw new NotImplementedException(); }
    public double? Fu_stomN { get => 123; set => throw new NotImplementedException(); }
    public string? Fu_stomT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsFu_stomPreset { get => true; set => throw new NotImplementedException(); }
    public double? Fu_duodN { get => 123; set => throw new NotImplementedException(); }
    public string? Fu_duodT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsFu_duodPreset { get => true; set => throw new NotImplementedException(); }
    public double? Fu_jejuN { get => 123; set => throw new NotImplementedException(); }
    public string? Fu_jejuT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsFu_jejuPreset { get => true; set => throw new NotImplementedException(); }
    public double? Fu_ileonN { get => 123; set => throw new NotImplementedException(); }
    public string? Fu_ileonT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsFu_ileonPreset { get => true; set => throw new NotImplementedException(); }
    public double? Fu_cecumN { get => 123; set => throw new NotImplementedException(); }
    public string? Fu_cecumT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsFu_cecumPreset { get => true; set => throw new NotImplementedException(); }
    public double? Fu_colonN { get => 123; set => throw new NotImplementedException(); }
    public string? Fu_colonT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsFu_colonPreset { get => true; set => throw new NotImplementedException(); }
    public double? Fu_plasmaN { get => 123; set => throw new NotImplementedException(); }
    public string? Fu_plasmaT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsFu_plasmaPreset { get => true; set => throw new NotImplementedException(); }
    public MolarMaximumRateOfReaction? Vmax_vitroN { get => MolarMaximumRateOfReaction.FromMicromolesPerMinutePerMilligram(123); set => throw new NotImplementedException(); }
    public string? Vmax_vitroT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsVmax_vitroPreset { get => true; set => throw new NotImplementedException(); }
    public Molarity? Km_vitroN { get => Molarity.FromMicromolesPerLiter(123); set => throw new NotImplementedException(); }
    public string? Km_vitroT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsKm_vitroPreset { get => true; set => throw new NotImplementedException(); }
    public VolumeFlow? Kle_kidN { get => VolumeFlow.FromLitersPerHour(123); set => throw new NotImplementedException(); }
    public string? Kle_kidT { get => "456"; set => throw new NotImplementedException(); }
    public bool IsKle_kidPreset { get => true; set => throw new NotImplementedException(); }
    public AmountOfSubstance? oral_dose_rate_magnitudeN { get => AmountOfSubstance.FromMicromoles(123); set => throw new NotImplementedException(); }
    public string? oral_dose_rate_magnitudeT { get => "456"; set => throw new NotImplementedException(); }
    public bool Isoral_dose_rate_magnitudePreset { get => true; set => throw new NotImplementedException(); }
    public Duration? oral_dose_rate_periodN { get => Duration.FromHours(123); set => throw new NotImplementedException(); }
    public string? oral_dose_rate_periodT { get => "456"; set => throw new NotImplementedException(); }
    public bool Isoral_dose_rate_periodPreset { get => true; set => throw new NotImplementedException(); }
    public Duration? oral_dose_rate_exposure_timeN { get => Duration.FromHours(123); set => throw new NotImplementedException(); }
    public string? oral_dose_rate_exposure_timeT { get => "456"; set => throw new NotImplementedException(); }
    public bool Isoral_dose_rate_exposure_timePreset { get => true; set => throw new NotImplementedException(); }
    public Duration? start_timeN { get => Duration.FromHours(123); set => throw new NotImplementedException(); }
    public string? start_timeT { get => "456"; set => throw new NotImplementedException(); }
    public bool Isstart_timePreset { get => true; set => throw new NotImplementedException(); }
    public Duration? end_timeN { get => Duration.FromHours(123); set => throw new NotImplementedException(); }
    public string? end_timeT { get => "456"; set => throw new NotImplementedException(); }
    public bool Isend_timePreset { get => true; set => throw new NotImplementedException(); }
    public Duration? time_stepN { get => Duration.FromHours(123); set => throw new NotImplementedException(); }
    public string? time_stepT { get => "456"; set => throw new NotImplementedException(); }
    public bool Istime_stepPreset { get => true; set => throw new NotImplementedException(); }

    public int SelectedIndex { get => 3; set => throw new NotImplementedException(); }

    public string Name => "Drug! Simple!";

    public bool CanConfigureSimulation => throw new NotImplementedException();

    public bool IsReady => true;

    public Option<Simulation> ConfigureSimulation(bool import)
    {
      throw new NotImplementedException();
    }

    public void Load(string pathToAcat)
    {
      throw new NotImplementedException();
    }
  }
}
