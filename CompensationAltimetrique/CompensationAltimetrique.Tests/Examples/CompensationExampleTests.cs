// ============================================================================
// TESTS DES EXEMPLES - COMPENSATION ALTIMÉTRIQUE
// Tests pour valider le bon fonctionnement des exemples d'utilisation
// ============================================================================

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using CompensationAltimetrique.Examples;

namespace CompensationAltimetrique.Tests.Examples
{
    [TestClass]
    public class CompensationExampleTests
    {
        [TestMethod]
        public void RunSimpleNetworkExample_ShouldExecuteWithoutException()
        {
            // Arrange
            var originalOut = Console.Out;
            using var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            try
            {
                // Act
                CompensationExample.RunSimpleNetworkExample();
                
                // Assert
                string output = stringWriter.ToString();
                Assert.IsFalse(string.IsNullOrEmpty(output));
                Assert.IsTrue(output.Contains("RÉSEAU DE NIVELLEMENT"));
                Assert.IsTrue(output.Contains("ALTITUDES ESTIMÉES"));
                Assert.IsTrue(output.Contains("ANALYSE STATISTIQUE"));
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [TestMethod]
        public void RunBlunderDetectionExample_ShouldExecuteWithoutException()
        {
            // Arrange
            var originalOut = Console.Out;
            using var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            try
            {
                // Act
                CompensationExample.RunBlunderDetectionExample();
                
                // Assert
                string output = stringWriter.ToString();
                Assert.IsFalse(string.IsNullOrEmpty(output));
                Assert.IsTrue(output.Contains("DÉTECTION DE FAUTE"));
                Assert.IsTrue(output.Contains("faute grossière"));
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [TestMethod]
        public void BothExamples_ShouldRunSuccessively()
        {
            // Arrange & Act - Vérifier qu'on peut exécuter les deux exemples à la suite
            try
            {
                CompensationExample.RunSimpleNetworkExample();
                CompensationExample.RunBlunderDetectionExample();
                
                // Assert - Si on arrive ici, pas d'exception
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Les exemples ont généré une exception: {ex.Message}");
            }
        }
    }
}