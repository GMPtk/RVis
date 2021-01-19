using Microsoft.VisualStudio.TestTools.UnitTesting;
using RVis.Client;
using RVis.Test;
using System.IO;
using System.Threading.Tasks;

namespace RVis.Model.Test
{
  [TestClass()]
  public class UnitTestManagedImport
  {
#if !IS_PIPELINES_BUILD
    [TestMethod()]
    public async Task TestInspectAsync()
    {
      // arrange
      var pathToLibrary = TestData.SimLibraryDirectory.FullName;
      var simLibrary = new SimLibrary();
      simLibrary.LoadFrom(pathToLibrary);
      var pathToCode = Path.Combine(pathToLibrary, "InspectTmpl", "inspect.R");

      // act
      using var managedImport = new ManagedImport(pathToCode, simLibrary);
      using var server = new RVisServer();
      var client = await server.OpenChannelAsync();
      managedImport.InspectAsync(client).Wait();

      // assert
      Assert.AreEqual(managedImport.Scalars.Count, 2);
      Assert.AreEqual(managedImport.ParameterCandidates.Count, 2);
      Assert.AreEqual(managedImport.DataSets.Count, 3);
      Assert.AreEqual(managedImport.ValueCandidates.Count, 3);
      Assert.AreEqual(managedImport.ValueCandidates.Single(vc => vc.Name == "o").ElementCandidates.Count, 3);
    }
#endif

#if !IS_PIPELINES_BUILD
    [TestMethod()]
    public async Task TestSetExecutorAsync()
    {
      // arrange
      var pathToLibrary = TestData.SimLibraryDirectory.FullName;
      var simLibrary = new SimLibrary();
      simLibrary.LoadFrom(pathToLibrary);
      var pathToCode = Path.Combine(pathToLibrary, "InspectExec", "inspect.R");

      // act
      using var managedImport = new ManagedImport(pathToCode, simLibrary);
      using var server = new RVisServer();
      var client = await server.OpenChannelAsync();
      managedImport.InspectAsync(client).Wait();
      managedImport.SetExecutorAsync(managedImport.UnaryFunctions[0], managedImport.ScalarSets[0], client).Wait();

      // assert
      Assert.AreEqual(managedImport.ExecutorOutput?.NColumns, 3);
      Assert.AreEqual(managedImport.ExecutorParameterCandidates.Count, 2);
      Assert.AreEqual(managedImport.ExecutorValueCandidates.Count, 2);
      Assert.AreEqual(managedImport.ScalarSets.Count, 1);
      Assert.AreEqual(managedImport.UnaryFunctions.Count, 1);
      Assert.AreEqual(managedImport.ValueCandidates.Count, 1);
    }
#endif

    //[TestMethod()]
    //public void TestImportExecToLibrary()
    //{
    //  Assert.Fail();
    //}

    //[TestMethod()]
    //public void TestImportTmplToLibrary()
    //{
    //  Assert.Fail();
    //}
  }
}