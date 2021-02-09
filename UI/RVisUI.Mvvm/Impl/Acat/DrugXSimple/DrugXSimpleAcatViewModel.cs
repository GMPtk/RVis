using LanguageExt;
using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using Scriban;
using Scriban.Runtime;
using System;
using System.IO;
using System.Text;
using UnitsNet;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVis.Model.Sim;
using static System.IO.Path;

namespace RVisUI.Mvvm
{
  public sealed class DrugXSimpleAcatViewModel : ReactiveObject, IDrugXSimpleAcatViewModel
  {
    public DrugXSimpleAcatViewModel(IAppService appService, SimLibrary simLibrary)
    {
      _appService = appService;
      _simLibrary = simLibrary;

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      this
        .GetWhenPropertyChanged()
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<string?>(
            ObservePropertyChanged
          )
        );
    }

    public MolarMass? MMN { get => _MMN; set => this.RaiseAndSetIfChanged(ref _MMN, value); }
    private MolarMass? _MMN;
    public string? MMT { get => _MMT; set => this.RaiseAndSetIfChanged(ref _MMT, CheckParse(nameof(MMT), value)); }
    private string? _MMT;
    public bool IsMMPreset { get => _isMMPreset; set => this.RaiseAndSetIfChanged(ref _isMMPreset, value); }
    private bool _isMMPreset;
    public Duration? TlagN { get => _TlagN; set => this.RaiseAndSetIfChanged(ref _TlagN, value); }
    private Duration? _TlagN;
    public string? TlagT { get => _TlagT; set => this.RaiseAndSetIfChanged(ref _TlagT, CheckParse(nameof(TlagT), value)); }
    private string? _TlagT;
    public bool IsTlagPreset { get => _isTlagPreset; set => this.RaiseAndSetIfChanged(ref _isTlagPreset, value); }
    private bool _isTlagPreset;
    public double? f_Abs_stomN { get => _f_Abs_stomN; set => this.RaiseAndSetIfChanged(ref _f_Abs_stomN, value); }
    private double? _f_Abs_stomN;
    public string? f_Abs_stomT { get => _f_Abs_stomT; set => this.RaiseAndSetIfChanged(ref _f_Abs_stomT, value.CheckParseValue<double>()); }
    private string? _f_Abs_stomT;
    public double? f_Abs_duodN { get => _f_Abs_duodN; set => this.RaiseAndSetIfChanged(ref _f_Abs_duodN, value); }
    private double? _f_Abs_duodN;
    public string? f_Abs_duodT { get => _f_Abs_duodT; set => this.RaiseAndSetIfChanged(ref _f_Abs_duodT, value.CheckParseValue<double>()); }
    private string? _f_Abs_duodT;
    public double? f_Abs_jejuN { get => _f_Abs_jejuN; set => this.RaiseAndSetIfChanged(ref _f_Abs_jejuN, value); }
    private double? _f_Abs_jejuN;
    public string? f_Abs_jejuT { get => _f_Abs_jejuT; set => this.RaiseAndSetIfChanged(ref _f_Abs_jejuT, value.CheckParseValue<double>()); }
    private string? _f_Abs_jejuT;
    public double? f_Abs_ileonN { get => _f_Abs_ileonN; set => this.RaiseAndSetIfChanged(ref _f_Abs_ileonN, value); }
    private double? _f_Abs_ileonN;
    public string? f_Abs_ileonT { get => _f_Abs_ileonT; set => this.RaiseAndSetIfChanged(ref _f_Abs_ileonT, value.CheckParseValue<double>()); }
    private string? _f_Abs_ileonT;
    public double? f_Abs_cecumN { get => _f_Abs_cecumN; set => this.RaiseAndSetIfChanged(ref _f_Abs_cecumN, value); }
    private double? _f_Abs_cecumN;
    public string? f_Abs_cecumT { get => _f_Abs_cecumT; set => this.RaiseAndSetIfChanged(ref _f_Abs_cecumT, value.CheckParseValue<double>()); }
    private string? _f_Abs_cecumT;
    public double? f_Abs_colonN { get => _f_Abs_colonN; set => this.RaiseAndSetIfChanged(ref _f_Abs_colonN, value); }
    private double? _f_Abs_colonN;
    public string? f_Abs_colonT { get => _f_Abs_colonT; set => this.RaiseAndSetIfChanged(ref _f_Abs_colonT, value.CheckParseValue<double>()); }
    private string? _f_Abs_colonT;
    public double? PeffN { get => _PeffN; set => this.RaiseAndSetIfChanged(ref _PeffN, value); }
    private double? _PeffN;
    public string? PeffT { get => _PeffT; set => this.RaiseAndSetIfChanged(ref _PeffT, value.CheckParseValue<double>()); }
    private string? _PeffT;
    public bool IsPeffPreset { get => _isPeffPreset; set => this.RaiseAndSetIfChanged(ref _isPeffPreset, value); }
    private bool _isPeffPreset;
    public double? Ratio_BPN { get => _Ratio_BPN; set => this.RaiseAndSetIfChanged(ref _Ratio_BPN, value); }
    private double? _Ratio_BPN;
    public string? Ratio_BPT { get => _Ratio_BPT; set => this.RaiseAndSetIfChanged(ref _Ratio_BPT, value.CheckParseValue<double>()); }
    private string? _Ratio_BPT;
    public bool IsRatio_BPPreset { get => _isRatio_BPPreset; set => this.RaiseAndSetIfChanged(ref _isRatio_BPPreset, value); }
    private bool _isRatio_BPPreset;
    public double? PC_adipN { get => _PC_adipN; set => this.RaiseAndSetIfChanged(ref _PC_adipN, value); }
    private double? _PC_adipN;
    public string? PC_adipT { get => _PC_adipT; set => this.RaiseAndSetIfChanged(ref _PC_adipT, value.CheckParseValue<double>()); }
    private string? _PC_adipT;
    public bool IsPC_adipPreset { get => _isPC_adipPreset; set => this.RaiseAndSetIfChanged(ref _isPC_adipPreset, value); }
    private bool _isPC_adipPreset;
    public double? PC_kidN { get => _PC_kidN; set => this.RaiseAndSetIfChanged(ref _PC_kidN, value); }
    private double? _PC_kidN;
    public string? PC_kidT { get => _PC_kidT; set => this.RaiseAndSetIfChanged(ref _PC_kidT, value.CheckParseValue<double>()); }
    private string? _PC_kidT;
    public bool IsPC_kidPreset { get => _isPC_kidPreset; set => this.RaiseAndSetIfChanged(ref _isPC_kidPreset, value); }
    private bool _isPC_kidPreset;
    public double? PC_rptN { get => _PC_rptN; set => this.RaiseAndSetIfChanged(ref _PC_rptN, value); }
    private double? _PC_rptN;
    public string? PC_rptT { get => _PC_rptT; set => this.RaiseAndSetIfChanged(ref _PC_rptT, value.CheckParseValue<double>()); }
    private string? _PC_rptT;
    public bool IsPC_rptPreset { get => _isPC_rptPreset; set => this.RaiseAndSetIfChanged(ref _isPC_rptPreset, value); }
    private bool _isPC_rptPreset;
    public double? PC_pptN { get => _PC_pptN; set => this.RaiseAndSetIfChanged(ref _PC_pptN, value); }
    private double? _PC_pptN;
    public string? PC_pptT { get => _PC_pptT; set => this.RaiseAndSetIfChanged(ref _PC_pptT, value.CheckParseValue<double>()); }
    private string? _PC_pptT;
    public bool IsPC_pptPreset { get => _isPC_pptPreset; set => this.RaiseAndSetIfChanged(ref _isPC_pptPreset, value); }
    private bool _isPC_pptPreset;
    public double? PC_liverN { get => _PC_liverN; set => this.RaiseAndSetIfChanged(ref _PC_liverN, value); }
    private double? _PC_liverN;
    public string? PC_liverT { get => _PC_liverT; set => this.RaiseAndSetIfChanged(ref _PC_liverT, value.CheckParseValue<double>()); }
    private string? _PC_liverT;
    public bool IsPC_liverPreset { get => _isPC_liverPreset; set => this.RaiseAndSetIfChanged(ref _isPC_liverPreset, value); }
    private bool _isPC_liverPreset;
    public double? PC_stomN { get => _PC_stomN; set => this.RaiseAndSetIfChanged(ref _PC_stomN, value); }
    private double? _PC_stomN;
    public string? PC_stomT { get => _PC_stomT; set => this.RaiseAndSetIfChanged(ref _PC_stomT, value.CheckParseValue<double>()); }
    private string? _PC_stomT;
    public bool IsPC_stomPreset { get => _isPC_stomPreset; set => this.RaiseAndSetIfChanged(ref _isPC_stomPreset, value); }
    private bool _isPC_stomPreset;
    public double? PC_duodN { get => _PC_duodN; set => this.RaiseAndSetIfChanged(ref _PC_duodN, value); }
    private double? _PC_duodN;
    public string? PC_duodT { get => _PC_duodT; set => this.RaiseAndSetIfChanged(ref _PC_duodT, value.CheckParseValue<double>()); }
    private string? _PC_duodT;
    public bool IsPC_duodPreset { get => _isPC_duodPreset; set => this.RaiseAndSetIfChanged(ref _isPC_duodPreset, value); }
    private bool _isPC_duodPreset;
    public double? PC_jejuN { get => _PC_jejuN; set => this.RaiseAndSetIfChanged(ref _PC_jejuN, value); }
    private double? _PC_jejuN;
    public string? PC_jejuT { get => _PC_jejuT; set => this.RaiseAndSetIfChanged(ref _PC_jejuT, value.CheckParseValue<double>()); }
    private string? _PC_jejuT;
    public bool IsPC_jejuPreset { get => _isPC_jejuPreset; set => this.RaiseAndSetIfChanged(ref _isPC_jejuPreset, value); }
    private bool _isPC_jejuPreset;
    public double? PC_ileonN { get => _PC_ileonN; set => this.RaiseAndSetIfChanged(ref _PC_ileonN, value); }
    private double? _PC_ileonN;
    public string? PC_ileonT { get => _PC_ileonT; set => this.RaiseAndSetIfChanged(ref _PC_ileonT, value.CheckParseValue<double>()); }
    private string? _PC_ileonT;
    public bool IsPC_ileonPreset { get => _isPC_ileonPreset; set => this.RaiseAndSetIfChanged(ref _isPC_ileonPreset, value); }
    private bool _isPC_ileonPreset;
    public double? PC_cecumN { get => _PC_cecumN; set => this.RaiseAndSetIfChanged(ref _PC_cecumN, value); }
    private double? _PC_cecumN;
    public string? PC_cecumT { get => _PC_cecumT; set => this.RaiseAndSetIfChanged(ref _PC_cecumT, value.CheckParseValue<double>()); }
    private string? _PC_cecumT;
    public bool IsPC_cecumPreset { get => _isPC_cecumPreset; set => this.RaiseAndSetIfChanged(ref _isPC_cecumPreset, value); }
    private bool _isPC_cecumPreset;
    public double? PC_colonN { get => _PC_colonN; set => this.RaiseAndSetIfChanged(ref _PC_colonN, value); }
    private double? _PC_colonN;
    public string? PC_colonT { get => _PC_colonT; set => this.RaiseAndSetIfChanged(ref _PC_colonT, value.CheckParseValue<double>()); }
    private string? _PC_colonT;
    public bool IsPC_colonPreset { get => _isPC_colonPreset; set => this.RaiseAndSetIfChanged(ref _isPC_colonPreset, value); }
    private bool _isPC_colonPreset;
    public double? Fu_adipN { get => _Fu_adipN; set => this.RaiseAndSetIfChanged(ref _Fu_adipN, value); }
    private double? _Fu_adipN;
    public string? Fu_adipT { get => _Fu_adipT; set => this.RaiseAndSetIfChanged(ref _Fu_adipT, value.CheckParseValue<double>()); }
    private string? _Fu_adipT;
    public bool IsFu_adipPreset { get => _isFu_adipPreset; set => this.RaiseAndSetIfChanged(ref _isFu_adipPreset, value); }
    private bool _isFu_adipPreset;
    public double? Fu_kidN { get => _Fu_kidN; set => this.RaiseAndSetIfChanged(ref _Fu_kidN, value); }
    private double? _Fu_kidN;
    public string? Fu_kidT { get => _Fu_kidT; set => this.RaiseAndSetIfChanged(ref _Fu_kidT, value.CheckParseValue<double>()); }
    private string? _Fu_kidT;
    public bool IsFu_kidPreset { get => _isFu_kidPreset; set => this.RaiseAndSetIfChanged(ref _isFu_kidPreset, value); }
    private bool _isFu_kidPreset;
    public double? Fu_pptN { get => _Fu_pptN; set => this.RaiseAndSetIfChanged(ref _Fu_pptN, value); }
    private double? _Fu_pptN;
    public string? Fu_pptT { get => _Fu_pptT; set => this.RaiseAndSetIfChanged(ref _Fu_pptT, value.CheckParseValue<double>()); }
    private string? _Fu_pptT;
    public bool IsFu_pptPreset { get => _isFu_pptPreset; set => this.RaiseAndSetIfChanged(ref _isFu_pptPreset, value); }
    private bool _isFu_pptPreset;
    public double? Fu_rptN { get => _Fu_rptN; set => this.RaiseAndSetIfChanged(ref _Fu_rptN, value); }
    private double? _Fu_rptN;
    public string? Fu_rptT { get => _Fu_rptT; set => this.RaiseAndSetIfChanged(ref _Fu_rptT, value.CheckParseValue<double>()); }
    private string? _Fu_rptT;
    public bool IsFu_rptPreset { get => _isFu_rptPreset; set => this.RaiseAndSetIfChanged(ref _isFu_rptPreset, value); }
    private bool _isFu_rptPreset;
    public double? Fu_bloodN { get => _Fu_bloodN; set => this.RaiseAndSetIfChanged(ref _Fu_bloodN, value); }
    private double? _Fu_bloodN;
    public string? Fu_bloodT { get => _Fu_bloodT; set => this.RaiseAndSetIfChanged(ref _Fu_bloodT, value.CheckParseValue<double>()); }
    private string? _Fu_bloodT;
    public bool IsFu_bloodPreset { get => _isFu_bloodPreset; set => this.RaiseAndSetIfChanged(ref _isFu_bloodPreset, value); }
    private bool _isFu_bloodPreset;
    public double? Fu_liverN { get => _Fu_liverN; set => this.RaiseAndSetIfChanged(ref _Fu_liverN, value); }
    private double? _Fu_liverN;
    public string? Fu_liverT { get => _Fu_liverT; set => this.RaiseAndSetIfChanged(ref _Fu_liverT, value.CheckParseValue<double>()); }
    private string? _Fu_liverT;
    public bool IsFu_liverPreset { get => _isFu_liverPreset; set => this.RaiseAndSetIfChanged(ref _isFu_liverPreset, value); }
    private bool _isFu_liverPreset;
    public double? Fu_portveinN { get => _Fu_portveinN; set => this.RaiseAndSetIfChanged(ref _Fu_portveinN, value); }
    private double? _Fu_portveinN;
    public string? Fu_portveinT { get => _Fu_portveinT; set => this.RaiseAndSetIfChanged(ref _Fu_portveinT, value.CheckParseValue<double>()); }
    private string? _Fu_portveinT;
    public bool IsFu_portveinPreset { get => _isFu_portveinPreset; set => this.RaiseAndSetIfChanged(ref _isFu_portveinPreset, value); }
    private bool _isFu_portveinPreset;
    public double? Fu_stomN { get => _Fu_stomN; set => this.RaiseAndSetIfChanged(ref _Fu_stomN, value); }
    private double? _Fu_stomN;
    public string? Fu_stomT { get => _Fu_stomT; set => this.RaiseAndSetIfChanged(ref _Fu_stomT, value.CheckParseValue<double>()); }
    private string? _Fu_stomT;
    public bool IsFu_stomPreset { get => _isFu_stomPreset; set => this.RaiseAndSetIfChanged(ref _isFu_stomPreset, value); }
    private bool _isFu_stomPreset;
    public double? Fu_duodN { get => _Fu_duodN; set => this.RaiseAndSetIfChanged(ref _Fu_duodN, value); }
    private double? _Fu_duodN;
    public string? Fu_duodT { get => _Fu_duodT; set => this.RaiseAndSetIfChanged(ref _Fu_duodT, value.CheckParseValue<double>()); }
    private string? _Fu_duodT;
    public bool IsFu_duodPreset { get => _isFu_duodPreset; set => this.RaiseAndSetIfChanged(ref _isFu_duodPreset, value); }
    private bool _isFu_duodPreset;
    public double? Fu_jejuN { get => _Fu_jejuN; set => this.RaiseAndSetIfChanged(ref _Fu_jejuN, value); }
    private double? _Fu_jejuN;
    public string? Fu_jejuT { get => _Fu_jejuT; set => this.RaiseAndSetIfChanged(ref _Fu_jejuT, value.CheckParseValue<double>()); }
    private string? _Fu_jejuT;
    public bool IsFu_jejuPreset { get => _isFu_jejuPreset; set => this.RaiseAndSetIfChanged(ref _isFu_jejuPreset, value); }
    private bool _isFu_jejuPreset;
    public double? Fu_ileonN { get => _Fu_ileonN; set => this.RaiseAndSetIfChanged(ref _Fu_ileonN, value); }
    private double? _Fu_ileonN;
    public string? Fu_ileonT { get => _Fu_ileonT; set => this.RaiseAndSetIfChanged(ref _Fu_ileonT, value.CheckParseValue<double>()); }
    private string? _Fu_ileonT;
    public bool IsFu_ileonPreset { get => _isFu_ileonPreset; set => this.RaiseAndSetIfChanged(ref _isFu_ileonPreset, value); }
    private bool _isFu_ileonPreset;
    public double? Fu_cecumN { get => _Fu_cecumN; set => this.RaiseAndSetIfChanged(ref _Fu_cecumN, value); }
    private double? _Fu_cecumN;
    public string? Fu_cecumT { get => _Fu_cecumT; set => this.RaiseAndSetIfChanged(ref _Fu_cecumT, value.CheckParseValue<double>()); }
    private string? _Fu_cecumT;
    public bool IsFu_cecumPreset { get => _isFu_cecumPreset; set => this.RaiseAndSetIfChanged(ref _isFu_cecumPreset, value); }
    private bool _isFu_cecumPreset;
    public double? Fu_colonN { get => _Fu_colonN; set => this.RaiseAndSetIfChanged(ref _Fu_colonN, value); }
    private double? _Fu_colonN;
    public string? Fu_colonT { get => _Fu_colonT; set => this.RaiseAndSetIfChanged(ref _Fu_colonT, value.CheckParseValue<double>()); }
    private string? _Fu_colonT;
    public bool IsFu_colonPreset { get => _isFu_colonPreset; set => this.RaiseAndSetIfChanged(ref _isFu_colonPreset, value); }
    private bool _isFu_colonPreset;
    public double? Fu_plasmaN { get => _Fu_plasmaN; set => this.RaiseAndSetIfChanged(ref _Fu_plasmaN, value); }
    private double? _Fu_plasmaN;
    public string? Fu_plasmaT { get => _Fu_plasmaT; set => this.RaiseAndSetIfChanged(ref _Fu_plasmaT, value.CheckParseValue<double>()); }
    private string? _Fu_plasmaT;
    public bool IsFu_plasmaPreset { get => _isFu_plasmaPreset; set => this.RaiseAndSetIfChanged(ref _isFu_plasmaPreset, value); }
    private bool _isFu_plasmaPreset;
    public MolarMaximumRateOfReaction? Vmax_vitroN { get => _Vmax_vitroN; set => this.RaiseAndSetIfChanged(ref _Vmax_vitroN, value); }
    private MolarMaximumRateOfReaction? _Vmax_vitroN;
    public string? Vmax_vitroT { get => _Vmax_vitroT; set => this.RaiseAndSetIfChanged(ref _Vmax_vitroT, CheckParse(nameof(Vmax_vitroT), value)); }
    private string? _Vmax_vitroT;
    public bool IsVmax_vitroPreset { get => _isVmax_vitroPreset; set => this.RaiseAndSetIfChanged(ref _isVmax_vitroPreset, value); }
    private bool _isVmax_vitroPreset;
    public Molarity? Km_vitroN { get => _Km_vitroN; set => this.RaiseAndSetIfChanged(ref _Km_vitroN, value); }
    private Molarity? _Km_vitroN;
    public string? Km_vitroT { get => _Km_vitroT; set => this.RaiseAndSetIfChanged(ref _Km_vitroT, CheckParse(nameof(Km_vitroT), value)); }
    private string? _Km_vitroT;
    public bool IsKm_vitroPreset { get => _isKm_vitroPreset; set => this.RaiseAndSetIfChanged(ref _isKm_vitroPreset, value); }
    private bool _isKm_vitroPreset;
    public VolumeFlow? Kle_kidN { get => _Kle_kidN; set => this.RaiseAndSetIfChanged(ref _Kle_kidN, value); }
    private VolumeFlow? _Kle_kidN;
    public string? Kle_kidT { get => _Kle_kidT; set => this.RaiseAndSetIfChanged(ref _Kle_kidT, CheckParse(nameof(Kle_kidT), value)); }
    private string? _Kle_kidT;
    public bool IsKle_kidPreset { get => _isKle_kidPreset; set => this.RaiseAndSetIfChanged(ref _isKle_kidPreset, value); }
    private bool _isKle_kidPreset;
    public AmountOfSubstance? oral_dose_rate_magnitudeN { get => _oral_dose_rate_magnitudeN; set => this.RaiseAndSetIfChanged(ref _oral_dose_rate_magnitudeN, value); }
    private AmountOfSubstance? _oral_dose_rate_magnitudeN;
    public string? oral_dose_rate_magnitudeT { get => _oral_dose_rate_magnitudeT; set => this.RaiseAndSetIfChanged(ref _oral_dose_rate_magnitudeT, CheckParse(nameof(oral_dose_rate_magnitudeT), value)); }
    private string? _oral_dose_rate_magnitudeT;
    public bool Isoral_dose_rate_magnitudePreset { get => _isoral_dose_rate_magnitudePreset; set => this.RaiseAndSetIfChanged(ref _isoral_dose_rate_magnitudePreset, value); }
    private bool _isoral_dose_rate_magnitudePreset;
    public Duration? oral_dose_rate_periodN { get => _oral_dose_rate_periodN; set => this.RaiseAndSetIfChanged(ref _oral_dose_rate_periodN, value); }
    private Duration? _oral_dose_rate_periodN;
    public string? oral_dose_rate_periodT { get => _oral_dose_rate_periodT; set => this.RaiseAndSetIfChanged(ref _oral_dose_rate_periodT, CheckParse(nameof(oral_dose_rate_periodT), value)); }
    private string? _oral_dose_rate_periodT;
    public bool Isoral_dose_rate_periodPreset { get => _isoral_dose_rate_periodPreset; set => this.RaiseAndSetIfChanged(ref _isoral_dose_rate_periodPreset, value); }
    private bool _isoral_dose_rate_periodPreset;
    public Duration? oral_dose_rate_exposure_timeN { get => _oral_dose_rate_exposure_timeN; set => this.RaiseAndSetIfChanged(ref _oral_dose_rate_exposure_timeN, value); }
    private Duration? _oral_dose_rate_exposure_timeN;
    public string? oral_dose_rate_exposure_timeT { get => _oral_dose_rate_exposure_timeT; set => this.RaiseAndSetIfChanged(ref _oral_dose_rate_exposure_timeT, CheckParse(nameof(oral_dose_rate_exposure_timeT), value)); }
    private string? _oral_dose_rate_exposure_timeT;
    public bool Isoral_dose_rate_exposure_timePreset { get => _isoral_dose_rate_exposure_timePreset; set => this.RaiseAndSetIfChanged(ref _isoral_dose_rate_exposure_timePreset, value); }
    private bool _isoral_dose_rate_exposure_timePreset;
    public Duration? start_timeN { get => _start_timeN; set => this.RaiseAndSetIfChanged(ref _start_timeN, value); }
    private Duration? _start_timeN;
    public string? start_timeT { get => _start_timeT; set => this.RaiseAndSetIfChanged(ref _start_timeT, CheckParse(nameof(start_timeT), value)); }
    private string? _start_timeT;
    public bool Isstart_timePreset { get => _isstart_timePreset; set => this.RaiseAndSetIfChanged(ref _isstart_timePreset, value); }
    private bool _isstart_timePreset;
    public Duration? end_timeN { get => _end_timeN; set => this.RaiseAndSetIfChanged(ref _end_timeN, value); }
    private Duration? _end_timeN;
    public string? end_timeT { get => _end_timeT; set => this.RaiseAndSetIfChanged(ref _end_timeT, CheckParse(nameof(end_timeT), value)); }
    private string? _end_timeT;
    public bool Isend_timePreset { get => _isend_timePreset; set => this.RaiseAndSetIfChanged(ref _isend_timePreset, value); }
    private bool _isend_timePreset;
    public Duration? time_stepN { get => _time_stepN; set => this.RaiseAndSetIfChanged(ref _time_stepN, value); }
    private Duration? _time_stepN;
    public string? time_stepT { get => _time_stepT; set => this.RaiseAndSetIfChanged(ref _time_stepT, CheckParse(nameof(time_stepT), value)); }
    private string? _time_stepT;
    public bool Istime_stepPreset { get => _istime_stepPreset; set => this.RaiseAndSetIfChanged(ref _istime_stepPreset, value); }
    private bool _istime_stepPreset;

    public int SelectedIndex
    {
      get => _selectedIndex;
      set => this.RaiseAndSetIfChanged(ref _selectedIndex, value);
    }
    private int _selectedIndex;

    public string Name => "Drug X (simple)";

    public bool CanConfigureSimulation
    {
      get => _canConfigureSimulation;
      set => this.RaiseAndSetIfChanged(ref _canConfigureSimulation, value);
    }
    private bool _canConfigureSimulation;

    public Option<Simulation> ConfigureSimulation(bool import) =>
      import ? DoImport() : DoLoad();

    public bool IsReady
    {
      get => _isReady;
      set => this.RaiseAndSetIfChanged(ref _isReady, value);
    }
    private bool _isReady;

    public void Load(string pathToAcat)
    {
      var pathToSimulation = Combine(pathToAcat, Name.ToValidFileName());
      if (!Directory.Exists(pathToSimulation))
      {
        Directory.CreateDirectory(pathToSimulation);
      }

      using var configStream = typeof(DrugXSimpleAcatViewModel).Assembly.GetManifestResourceStream(
        "RVisUI.Mvvm.Impl.Acat.DrugXSimple.config.toml"
        );
      RequireNotNull(configStream);
      var configT = Nett.Toml.ReadStream<TSimConfig>(configStream);
      var config = FromToml(configT);
      RequireNotNullEmptyWhiteSpace(config.SimCode.File);

      var userParameterSet = _userParameterSetNames.Map(
        n => config.SimInput.SimParameters
          .FindParameter(n)
          .AssertSome($"Config missing user parameter {n}")
        );

      var userParameterPresetNames = config.SimInput.SimParameters
        .Filter(p => !_userParameterSetNames.Contains(p.Name));

      var userParameterPresets = MakePresets(userParameterPresetNames);

      var constants = config.SimInput.SimParameters.Filter(
        p => !(userParameterSet.Exists(q => q.Name == p.Name) || userParameterPresets.Exists(q => q.Parameter.Name == p.Name))
        );

      using var inStream = typeof(DrugXSimpleAcatViewModel).Assembly.GetManifestResourceStream(
        "RVisUI.Mvvm.Impl.Acat.DrugXSimple.ACAT_like.in"
        );
      RequireNotNull(inStream);
      using var inReader = new StreamReader(inStream, Encoding.UTF8, true);
      var inContent = inReader.ReadToEnd();
      var inTemplate = Template.ParseLiquid(inContent);

      var pathToExe = Combine(pathToAcat, config.SimCode.File);
      if (!File.Exists(pathToExe)) return;

      _pathToAcat = pathToAcat;
      _pathToSimulation = pathToSimulation;
      _config = config;
      _userParameterSet = userParameterSet;
      _userParameterPresets = userParameterPresets;
      _constants = constants;
      _inTemplate = inTemplate;
      
      IsReady = true;

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        _userParameterPresets.Iter(uppn =>
        {
          uppn.SetterT(this, uppn.Parameter.Value);
          uppn.SetterN(this, uppn.Parameter.Scalar);
          uppn.IsPresetSetter(this, true);
        });
      }

      UpdateCanConfigureSimulation();
    }

    private void ObservePropertyChanged(string? propertyName)
    {
      var didUpdate = UpdateNProperty(propertyName);

      if (didUpdate) UpdateCanConfigureSimulation();
    }

    private bool UpdateNProperty(string? propertyName)
    {
      if (propertyName?.EndsWith('T') != true) return false;

      var parameterName = propertyName[..^1];

      var uppns = _userParameterPresets.Filter(
        uppn => uppn.Parameter.Name == parameterName
        );

      if (uppns.IsEmpty) return false;

      var (_, _, _, _, _, _, _, _, getTsetN) = uppns.Single();

      getTsetN(this);

      return true;
    }

    private
    Arr<(
      SimParameter Parameter,
      Action<DrugXSimpleAcatViewModel, string?> SetterT,
      Func<DrugXSimpleAcatViewModel, string?> GetterT,
      Action<DrugXSimpleAcatViewModel, double?> SetterN,
      Func<DrugXSimpleAcatViewModel, double?> GetterN,
      Action<DrugXSimpleAcatViewModel, bool> IsPresetSetter,
      Func<DrugXSimpleAcatViewModel, bool> IsPresetGetter,
      Func<string?, object?> Parser,
      Action<DrugXSimpleAcatViewModel> GetTSetN
      )>
    MakePresets(Arr<SimParameter> parameters)
    {
      var properties = GetType().GetProperties().ToArr();

      return parameters.Map(parameter =>
      {
        var nsProperty = properties
          .Find(p => p.Name == $"{parameter.Name}T")
          .AssertSome($"Failed to find {parameter.Name}T");

        void setterT(DrugXSimpleAcatViewModel vm, string? s)
        {
          nsProperty.SetValue(vm, s);
        }

        string? getterT(DrugXSimpleAcatViewModel vm)
        {
          return (string?)nsProperty.GetValue(vm);
        }

        var ndProperty = properties
          .Find(p => p.Name == $"{parameter.Name}N")
          .AssertSome($"Failed to find {parameter.Name}N");

        void setterN(DrugXSimpleAcatViewModel vm, double? d)
        {
          if (ndProperty.PropertyType == typeof(double?))
          {
            ndProperty.SetValue(vm, d);
            return;
          }

          if (d is null)
          {
            ndProperty.SetValue(vm, null);
            return;
          }

          RequireNotNullEmptyWhiteSpace(
            parameter.Unit,
            $"Expecting {parameter.Name} parameter to have unit specified"
          );

          if (ndProperty.PropertyType == typeof(Duration?))
          {
            var unit = Duration.ParseUnit(parameter.Unit);
            ndProperty.SetValue(vm, Duration.From(d.Value, unit));
          }
          else if (ndProperty.PropertyType == typeof(VolumeFlow?))
          {
            var unit = VolumeFlow.ParseUnit(parameter.Unit);
            ndProperty.SetValue(vm, VolumeFlow.From(d.Value, unit));
          }
          else if (ndProperty.PropertyType == typeof(MolarMass?))
          {
            var unit = MolarMass.ParseUnit(parameter.Unit);
            ndProperty.SetValue(vm, MolarMass.From(d.Value, unit));
          }
          else if (ndProperty.PropertyType == typeof(MolarMaximumRateOfReaction?))
          {
            var unit = MolarMaximumRateOfReaction.ParseUnit(parameter.Unit);
            ndProperty.SetValue(vm, MolarMaximumRateOfReaction.From(d.Value, unit));
          }
          else if (ndProperty.PropertyType == typeof(Molarity?))
          {
            var unit = Molarity.ParseUnit(parameter.Unit);
            ndProperty.SetValue(vm, Molarity.From(d.Value, unit));
          }
          else if (ndProperty.PropertyType == typeof(AmountOfSubstance?))
          {
            var unit = AmountOfSubstance.ParseUnit(parameter.Unit);
            ndProperty.SetValue(vm, AmountOfSubstance.From(d.Value, unit));
          }
          else
          {
            throw new InvalidOperationException($"Unsupported UoM: {ndProperty.PropertyType.Name}");
          }
        }

        double? getterN(DrugXSimpleAcatViewModel vm)
        {
          var v = ndProperty.GetValue(vm);
          if (v is null) return null;

          if (ndProperty.PropertyType == typeof(double?))
          {
            return (double)v;
          }

          RequireNotNullEmptyWhiteSpace(
            parameter.Unit,
            $"Expecting {parameter.Name} parameter to have unit specified"
          );

          return v switch
          {
            Duration d => d.As(Duration.ParseUnit(parameter.Unit)),
            VolumeFlow vf => vf.As(VolumeFlow.ParseUnit(parameter.Unit)),
            MolarMass mm => mm.As(MolarMass.ParseUnit(parameter.Unit)),
            MolarMaximumRateOfReaction mmrr => mmrr.As(MolarMaximumRateOfReaction.ParseUnit(parameter.Unit)),
            Molarity m => m.As(Molarity.ParseUnit(parameter.Unit)),
            AmountOfSubstance aos => aos.As(AmountOfSubstance.ParseUnit(parameter.Unit)),
            _ => throw new InvalidOperationException($"Unsupported UoM: {v.GetType().Name}")
          };
        }

        var bProperty = properties
          .Find(p => p.Name == $"Is{parameter.Name}Preset");

        void isPresetSetter(DrugXSimpleAcatViewModel vm, bool b)
        {
          bProperty.IfSome(p => p.SetValue(vm, b));
        }

        bool isPresetGetter(DrugXSimpleAcatViewModel vm)
        {
          return bProperty.Match(p => (bool)p.GetValue(vm)!, () => true);
        }

        object? parser(string? s)
        {
          if (s is null) return null;
          if (double.TryParse(s, out double d)) return d;

          if (ndProperty.PropertyType == typeof(Duration?))
          {
            try
            {
              return Duration.Parse(s);
            }
            catch (Exception)
            {
              throw new ArgumentException("Valid time required(e.g. 60s, 0.5 h)");
            }
          }
          else if (ndProperty.PropertyType == typeof(VolumeFlow?))
          {
            try
            {
              return VolumeFlow.Parse(s);
            }
            catch (Exception)
            {
              throw new ArgumentException($"Valid flow required (e.g. 2 L/h, 3 cm³/min)");
            }
          }
          else if (ndProperty.PropertyType == typeof(MolarMass?))
          {
            try
            {
              return MolarMass.Parse(s);
            }
            catch (Exception)
            {
              throw new ArgumentException($"Valid molar mass required (e.g. 2 g/mol)");
            }
          }
          else if (ndProperty.PropertyType == typeof(MolarMaximumRateOfReaction?))
          {
            try
            {
              return MolarMaximumRateOfReaction.Parse(s);
            }
            catch (Exception)
            {
              throw new ArgumentException($"Valid max rate of reaction required (e.g. 2 mmol/h/g)");
            }
          }
          else if (ndProperty.PropertyType == typeof(Molarity?))
          {
            try
            {
              return Molarity.Parse(s);
            }
            catch (Exception)
            {
              throw new ArgumentException($"Valid molarity required (e.g. 2 mg/L)");
            }
          }
          else if (ndProperty.PropertyType == typeof(AmountOfSubstance?))
          {
            try
            {
              return AmountOfSubstance.Parse(s);
            }
            catch (Exception)
            {
              throw new ArgumentException($"Valid quantity required (e.g. 0.2 mmol)");
            }
          }
          else
          {
            throw new InvalidOperationException($"Unsupported UoM: {ndProperty.PropertyType.Name}");
          }
        }

        void getTSetN(DrugXSimpleAcatViewModel vm)
        {
          var t = getterT(vm);
          var o = parser(t);
          if (o is double d)
          {
            setterN(vm, d);
          }
          else
          {
            ndProperty.SetValue(vm, o);
          }
        }

        return (
          parameter,
          (Action<DrugXSimpleAcatViewModel, string?>)setterT,
          (Func<DrugXSimpleAcatViewModel, string?>)getterT,
          (Action<DrugXSimpleAcatViewModel, double?>)setterN,
          (Func<DrugXSimpleAcatViewModel, double?>)getterN,
          (Action<DrugXSimpleAcatViewModel, bool>)isPresetSetter,
          (Func<DrugXSimpleAcatViewModel, bool>)isPresetGetter,
          (Func<string?, object?>)parser,
          (Action<DrugXSimpleAcatViewModel>)getTSetN
          );
      });
    }

    private string? CheckParse(string propertyName, string? s)
    {
      var parameter = propertyName[..^1];
      var (_, _, _, _, _, _, _, parser, _) = _userParameterPresets.Single(upp => upp.Parameter.Name == parameter);
      var _ = parser(s);
      return s;
    }

    private (Arr<(SimParameter Parameter, double Value)> Presets, Arr<(SimParameter Parameter, double Value)> NonPresets) GetUserConfiguration()
    {
      RequireTrue(CanConfigureSimulation);

      var userConfiguration = _userParameterPresets.Map(
        uppn => (uppn.Parameter, Value: uppn.GetterN(this), IsPreset: uppn.IsPresetGetter(this))
        );

      var presets = userConfiguration
        .Filter(uc => uc.IsPreset)
        .Map(uc => (uc.Parameter, uc.Value!.Value));

      var nonPresets = userConfiguration
        .Filter(uc => !uc.IsPreset)
        .Map(uc => (uc.Parameter, uc.Value!.Value));

      return (presets, nonPresets);
    }

    private void ConfigureSimulationDirectory(
      string pathToSimulation,
      Arr<(SimParameter Parameter, double Value)> presets, 
      Arr<(SimParameter Parameter, double Value)> nonPresets,
      string inFileName
      )
    {
      RequireDirectory(_pathToAcat);
      RequireNotNull(_config.SimCode.File);
      RequireNotNull(_inTemplate);

      Directory.CreateDirectory(pathToSimulation);

      var pathToExeSource = Combine(_pathToAcat, _config.SimCode.File);
      RequireFile(pathToExeSource);
      var pathToExeDestination = Combine(pathToSimulation, _config.SimCode.File);
      File.Copy(pathToExeSource, pathToExeDestination);

      var scriptObject = new ScriptObject();

      _userParameterSetNames.Iter(
        n => scriptObject.Add(n, $"{{{{{n}}}}}")
        );

      _constants.Iter(
        c => scriptObject.Add(c.Name, c.Value)
        );

      presets.Iter(
        p => scriptObject.Add(p.Parameter.Name, p.Value)
        );

      nonPresets.Iter(
        up => scriptObject.Add(up.Parameter.Name, $"{{{{{up.Parameter.Name}}}}}")
        );

      var context = new TemplateContext();
      context.PushGlobal(scriptObject);

      var inContent = _inTemplate.Render(context);

      var pathToInFile = Combine(pathToSimulation, inFileName);
      File.WriteAllText(pathToInFile, inContent);
    }

    private Option<Simulation> DoImport()
    {
      var dialogVM = new SimulationLabelViewModel();

      var ok = _appService.ShowDialog(dialogVM, null);

      if (!ok) return None;

      RequireNotNullEmptyWhiteSpace(dialogVM.Name);

      var directoryName = dialogVM.Name.ToValidFileName();

      if (directoryName.IsntAString())
      {
        _appService.Notify(
          NotificationType.Error,
          nameof(DrugXSimpleAcatViewModel),
          nameof(DoImport),
          $"{dialogVM.Name} is not a valid name"
          );

        return None;
      }

      var pathToSimulation = Combine(GetTempPath(), directoryName);

      var (presets, nonPresets) = GetUserConfiguration();

      ConfigureSimulationDirectory(pathToSimulation, presets, nonPresets, "ACAT_like.in");

      var nonPresetParameters = nonPresets.Map(
        np => np.Parameter.With(np.Value)
        );

      var input = new SimInput(_userParameterSet + nonPresetParameters, isDefault: true);

      var config = new SimConfig(
        dialogVM.Name,
        dialogVM.Description,
        DateTime.UtcNow,
        _config.SimCode,
        input,
        _config.SimOutput
        );

      config.WriteToFile(pathToSimulation);

      directoryName = _simLibrary.ImportExeSimulation(pathToSimulation, directoryName);

      _simLibrary.Refresh();

      var lookup = $"{DirectorySeparatorChar}{directoryName}";

      return _simLibrary.Simulations.Find(
        s => s.PathToSimulation.EndsWith(lookup)
        );
    }

    private Option<Simulation> DoLoad()
    {
      RequireDirectory(_pathToSimulation);

      var (presets, nonPresets) = GetUserConfiguration();

      var hashSource = presets.Aggregate(
        new StringBuilder(), (sb, t) => sb.Append(t.Value)
        );
      var hash = hashSource.ToString().ToHash();

      var pathToSimulation = Combine(_pathToSimulation, hash);

      if (Directory.Exists(pathToSimulation))
      {
        return Simulation.LoadFrom(pathToSimulation);
      }

      ConfigureSimulationDirectory(pathToSimulation, presets, nonPresets, "ACAT_like.in");

      var nonPresetParameters = nonPresets.Map(
        np => np.Parameter.With(np.Value)
        );

      var input = new SimInput(_userParameterSet + nonPresetParameters, isDefault: true);
      var config = _config.With(input);
      config.WriteToFile(pathToSimulation);

      return new Simulation(pathToSimulation, config);
    }

    private void UpdateCanConfigureSimulation()
    {
      var allNonNull = _userParameterPresets
        .Map(uppn => uppn.GetterN(this))
        .ForAll(nd => nd.HasValue);

      CanConfigureSimulation = allNonNull && MMN?.GramsPerMole > 0d;
    }

    private readonly IAppService _appService;
    private readonly SimLibrary _simLibrary;
    private readonly Arr<string> _userParameterSetNames = Array("BDM");
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private string? _pathToAcat;
    public bool IspathToAcaPreset { get => _ispathToAcaPreset; set => this.RaiseAndSetIfChanged(ref _ispathToAcaPreset, value); }
    private bool _ispathToAcaPreset;
    private string? _pathToSimulation;
    private SimConfig _config;
    private Arr<SimParameter> _userParameterSet;
    private Arr<SimParameter> _constants;
    private Arr<(
      SimParameter Parameter,
      Action<DrugXSimpleAcatViewModel, string?> SetterT,
      Func<DrugXSimpleAcatViewModel, string?> GetterT,
      Action<DrugXSimpleAcatViewModel, double?> SetterN,
      Func<DrugXSimpleAcatViewModel, double?> GetterN,
      Action<DrugXSimpleAcatViewModel, bool> IsPresetSetter,
      Func<DrugXSimpleAcatViewModel, bool> IsPresetGetter,
      Func<string?, object?> Parser,
      Action<DrugXSimpleAcatViewModel> GetTSetN
      )> _userParameterPresets;
    private Template? _inTemplate;
  }
}
