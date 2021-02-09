using UnitsNet;

namespace RVisUI.Mvvm
{
  public interface IDrugXSimpleAcatViewModel : IAcatViewModel
  {
    MolarMass? MMN { get; set; }
    string? MMT { get; set; }
    bool IsMMPreset { get; set; }

    Duration? TlagN { get; set; }
    string? TlagT { get; set; }
    bool IsTlagPreset { get; set; }

    double? f_Abs_stomN { get; set; }
    string? f_Abs_stomT { get; set; }

    double? f_Abs_duodN { get; set; }
    string? f_Abs_duodT { get; set; }

    double? f_Abs_jejuN { get; set; }
    string? f_Abs_jejuT { get; set; }

    double? f_Abs_ileonN { get; set; }
    string? f_Abs_ileonT { get; set; }

    double? f_Abs_cecumN { get; set; }
    string? f_Abs_cecumT { get; set; }

    double? f_Abs_colonN { get; set; }
    string? f_Abs_colonT { get; set; }

    double? PeffN { get; set; }
    string? PeffT { get; set; }
    bool IsPeffPreset { get; set; }

    double? Ratio_BPN { get; set; }
    string? Ratio_BPT { get; set; }
    bool IsRatio_BPPreset { get; set; }

    double? PC_adipN { get; set; }
    string? PC_adipT { get; set; }
    bool IsPC_adipPreset { get; set; }

    double? PC_kidN { get; set; }
    string? PC_kidT { get; set; }
    bool IsPC_kidPreset { get; set; }

    double? PC_rptN { get; set; }
    string? PC_rptT { get; set; }
    bool IsPC_rptPreset { get; set; }

    double? PC_pptN { get; set; }
    string? PC_pptT { get; set; }
    bool IsPC_pptPreset { get; set; }

    double? PC_liverN { get; set; }
    string? PC_liverT { get; set; }
    bool IsPC_liverPreset { get; set; }

    double? PC_stomN { get; set; }
    string? PC_stomT { get; set; }
    bool IsPC_stomPreset { get; set; }

    double? PC_duodN { get; set; }
    string? PC_duodT { get; set; }
    bool IsPC_duodPreset { get; set; }

    double? PC_jejuN { get; set; }
    string? PC_jejuT { get; set; }
    bool IsPC_jejuPreset { get; set; }

    double? PC_ileonN { get; set; }
    string? PC_ileonT { get; set; }
    bool IsPC_ileonPreset { get; set; }

    double? PC_cecumN { get; set; }
    string? PC_cecumT { get; set; }
    bool IsPC_cecumPreset { get; set; }

    double? PC_colonN { get; set; }
    string? PC_colonT { get; set; }
    bool IsPC_colonPreset { get; set; }

    double? Fu_adipN { get; set; }
    string? Fu_adipT { get; set; }
    bool IsFu_adipPreset { get; set; }

    double? Fu_kidN { get; set; }
    string? Fu_kidT { get; set; }
    bool IsFu_kidPreset { get; set; }

    double? Fu_pptN { get; set; }
    string? Fu_pptT { get; set; }
    bool IsFu_pptPreset { get; set; }

    double? Fu_rptN { get; set; }
    string? Fu_rptT { get; set; }
    bool IsFu_rptPreset { get; set; }

    double? Fu_bloodN { get; set; }
    string? Fu_bloodT { get; set; }
    bool IsFu_bloodPreset { get; set; }

    double? Fu_liverN { get; set; }
    string? Fu_liverT { get; set; }
    bool IsFu_liverPreset { get; set; }

    double? Fu_portveinN { get; set; }
    string? Fu_portveinT { get; set; }
    bool IsFu_portveinPreset { get; set; }

    double? Fu_stomN { get; set; }
    string? Fu_stomT { get; set; }
    bool IsFu_stomPreset { get; set; }

    double? Fu_duodN { get; set; }
    string? Fu_duodT { get; set; }
    bool IsFu_duodPreset { get; set; }

    double? Fu_jejuN { get; set; }
    string? Fu_jejuT { get; set; }
    bool IsFu_jejuPreset { get; set; }

    double? Fu_ileonN { get; set; }
    string? Fu_ileonT { get; set; }
    bool IsFu_ileonPreset { get; set; }

    double? Fu_cecumN { get; set; }
    string? Fu_cecumT { get; set; }
    bool IsFu_cecumPreset { get; set; }

    double? Fu_colonN { get; set; }
    string? Fu_colonT { get; set; }
    bool IsFu_colonPreset { get; set; }

    double? Fu_plasmaN { get; set; }
    string? Fu_plasmaT { get; set; }
    bool IsFu_plasmaPreset { get; set; }

    MolarMaximumRateOfReaction? Vmax_vitroN { get; set; }
    string? Vmax_vitroT { get; set; }
    bool IsVmax_vitroPreset { get; set; }

    Molarity? Km_vitroN { get; set; }
    string? Km_vitroT { get; set; }
    bool IsKm_vitroPreset { get; set; }

    VolumeFlow? Kle_kidN { get; set; }
    string? Kle_kidT { get; set; }
    bool IsKle_kidPreset { get; set; }

    AmountOfSubstance? oral_dose_rate_magnitudeN { get; set; }
    string? oral_dose_rate_magnitudeT { get; set; }
    bool Isoral_dose_rate_magnitudePreset { get; set; }

    Duration? oral_dose_rate_periodN { get; set; }
    string? oral_dose_rate_periodT { get; set; }
    bool Isoral_dose_rate_periodPreset { get; set; }

    Duration? oral_dose_rate_exposure_timeN { get; set; }
    string? oral_dose_rate_exposure_timeT { get; set; }
    bool Isoral_dose_rate_exposure_timePreset { get; set; }

    Duration? start_timeN { get; set; }
    string? start_timeT { get; set; }
    bool Isstart_timePreset { get; set; }

    Duration? end_timeN { get; set; }
    string? end_timeT { get; set; }
    bool Isend_timePreset { get; set; }

    Duration? time_stepN { get; set; }
    string? time_stepT { get; set; }
    bool Istime_stepPreset { get; set; }

    int SelectedIndex { get; set; }
  }

  public interface IDrugXNotSoSimpleAcatViewModel : IAcatViewModel
  {

  }
}
