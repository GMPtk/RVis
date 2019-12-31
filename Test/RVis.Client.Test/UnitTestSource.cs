using Microsoft.VisualStudio.TestTools.UnitTesting;
using RVis.Base.Extensions;
using RVis.Data;
using System;
using System.Linq;

namespace RVis.Client.Test
{
  [TestClass]
  public class UnitTestSource
  {
#if !IS_PIPELINES_BUILD
    [TestMethod]
    public void TestEvaluate()
    {
      // arrange
      var invalidR = "seq({i * 10}, {i*30}, by={i*10})";
      var validR = "1:3";

      string invalidRMessage = default;
      NumDataColumn[] validRReturn;

      // act
      using (var server = new RVisServer())
      {
        using (var client = server.OpenChannel())
        {
          // invalid first to show channel is not left faulted
          try
          {
            client.EvaluateNumData(invalidR);
          }
          catch (Exception ex)
          {
            invalidRMessage = ex.Message;
          }

          validRReturn = client.EvaluateNumData(validR);
        }
      }

      // assert
      Assert.IsTrue(invalidRMessage.Contains("'i' not found"));
      Assert.IsTrue(new[] { 1.0, 2, 3 }.SequenceEqual(validRReturn[0].Data.ToArray()));
    }
#endif

#if !IS_PIPELINES_BUILD
    [TestMethod]
    public void TestCreateVector()
    {
      // arrange
      var source01 = new double[] { };
      var source02 = new[] { 1d, 2, 3 };

      double[] expected01 = default;
      var expected02 = source02;

      double[] actual01;
      double[] actual02;

      // act
      using (var server = new RVisServer())
      {
        using var client = server.OpenChannel();

        client.CreateVector(source01, nameof(source01));
        client.CreateVector(source02, nameof(source02));

        var doubles = client.EvaluateDoubles(nameof(source01));
        actual01 = doubles.Single().Value?.ToArray();

        doubles = client.EvaluateDoubles(nameof(source02));
        actual02 = doubles.Single().Value?.ToArray();
      }

      // assert
      Assert.AreEqual(expected01, actual01);
      Assert.IsTrue(expected02.SequenceEqual(actual02));
    }
#endif

#if !IS_PIPELINES_BUILD
    [TestMethod]
    public void TestCreateMatrix()
    {
      // arrange
      var source01 = new double[0, 0] { };
      var source02 = new double[3, 3] { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 } };

      double[,] expected01 = default;
      var expected02 = source02.ToJagged();

      double[][] actual01;
      double[][] actual02;

      // act
      using (var server = new RVisServer())
      {
        using var client = server.OpenChannel();

        client.CreateMatrix(source01.ToJagged(), nameof(source01));
        client.CreateMatrix(source02.ToJagged(), nameof(source02));

        var doubles = client.EvaluateDoubles(nameof(source01));
        actual01 = doubles?.Select(ds => ds.Value?.ToArray()).ToArray();

        doubles = client.EvaluateDoubles(nameof(source02));
        actual02 = doubles?
          .Select(ds => ds.Value?.ToArray())
          .ToArray()
          .Transpose();
      }

      // assert
      Assert.AreEqual(expected01, actual01);
      expected02.Iter((i,a) => Assert.IsTrue(a.SequenceEqual(actual02[i])));
    }
#endif
  }
}
