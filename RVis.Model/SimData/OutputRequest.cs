using LanguageExt;
using System;
using static LanguageExt.Prelude;

namespace RVis.Model
{
  public class OutputRequest
  {
    public static OutputRequest Create(SimInput seriesInput, bool persist) =>
      new OutputRequest(seriesInput, Either<Exception, Arr<SimInput>>.Bottom, persist);

    public static OutputRequest Create(SimInput seriesInput, Exception exception) =>
      new OutputRequest(seriesInput, Left(exception), false);

    public static OutputRequest Create(SimInput seriesInput, Arr<SimInput> serieInputs) =>
      new OutputRequest(seriesInput, Right<Exception, Arr<SimInput>>(serieInputs), false);

    private OutputRequest(SimInput seriesInput, Either<Exception, Arr<SimInput>> serieInputs, bool persist)
    {
      SeriesInput = seriesInput;
      SerieInputs = serieInputs;
      Persist = persist;
    }

    public SimInput SeriesInput { get; }

    public Either<Exception, Arr<SimInput>> SerieInputs { get; }

    public bool Persist { get; }
  }
}
