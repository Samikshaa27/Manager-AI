using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanAI.Agents;

namespace PlanAI.Tests.Agents
{
    [TestClass]
    public class CategoryDetectorAgentTests
    {
        [TestMethod]
        public void DetectCategory_SolarProject_ReturnsSolarWithHighConfidence()
        {
            // Arrange
            var description = "Install solar panels on factory roof with inverters and monitoring system";

            // Act
            var (category, confidence) = CategoryDetectorAgent.DetectCategory(description);

            // Assert
            Assert.AreEqual("Solar", category, "Category should be detected as Solar");
            Assert.IsTrue(confidence >= 70, $"Confidence should be >= 70%, got {confidence}%");
        }

        [TestMethod]
        public void DetectCategory_SoftwareProject_ReturnsSoftwareWithHighConfidence()
        {
            // Arrange
            var description = "Build a web application backend API and React frontend dashboard for project management";

            // Act
            var (category, confidence) = CategoryDetectorAgent.DetectCategory(description);

            // Assert
            Assert.AreEqual("Software", category, "Category should be detected as Software");
            Assert.IsTrue(confidence >= 70, $"Confidence should be >= 70%, got {confidence}%");
        }

        [TestMethod]
        public void DetectCategory_ConstructionProject_ReturnsConstructionWithHighConfidence()
        {
            // Arrange
            var description = "Build a new office building with foundation, construction site management, and contractor coordination";

            // Act
            var (category, confidence) = CategoryDetectorAgent.DetectCategory(description);

            // Assert
            Assert.AreEqual("Construction", category, "Category should be detected as Construction");
            Assert.IsTrue(confidence >= 70, $"Confidence should be >= 70%, got {confidence}%");
        }

        [TestMethod]
        public void DetectCategory_UnknownProject_ReturnsOtherWithLowConfidence()
        {
            // Arrange
            var description = "Organize a team dinner and socializing event for company morale";

            // Act
            var (category, confidence) = CategoryDetectorAgent.DetectCategory(description);

            // Assert
            Assert.AreEqual("Other", category, "Category should default to Other for unknown projects");
            Assert.IsTrue(confidence <= 50, $"Confidence should be low for unknown category, got {confidence}%");
        }

        [TestMethod]
        public void DetectCategory_HealthcareProject_ReturnsHealthcareWithHighConfidence()
        {
            // Arrange
            var description = "Set up a telemedicine clinic with patient records system and medical equipment";

            // Act
            var (category, confidence) = CategoryDetectorAgent.DetectCategory(description);

            // Assert
            Assert.AreEqual("Healthcare", category, "Category should be detected as Healthcare");
            Assert.IsTrue(confidence >= 70, $"Confidence should be >= 70%, got {confidence}%");
        }

        [TestMethod]
        public void DetectCategory_EmptyInput_ThrowsArgumentException()
        {
            // Arrange
            var description = "";

            // Act & Assert
            var ex = Assert.ThrowsException<ArgumentException>(() =>
                CategoryDetectorAgent.DetectCategory(description));

            Assert.IsTrue(ex.Message.Contains("empty"), "Exception message should mention empty description");
        }

        [TestMethod]
        public void DetectCategory_NullInput_ThrowsArgumentException()
        {
            // Arrange
            string description = null;

            // Act & Assert
            var ex = Assert.ThrowsException<ArgumentException>(() =>
                CategoryDetectorAgent.DetectCategory(description));

            Assert.IsTrue(ex.Message.Contains("empty"), "Exception message should mention empty description");
        }

        [TestMethod]
        public void DetectCategory_ManufacturingProject_ReturnsManufacturingWithHighConfidence()
        {
            // Arrange
            var description = "Set up a manufacturing plant with assembly line and industrial production automation";

            // Act
            var (category, confidence) = CategoryDetectorAgent.DetectCategory(description);

            // Assert
            Assert.AreEqual("Manufacturing", category, "Category should be detected as Manufacturing");
            Assert.IsTrue(confidence >= 70, $"Confidence should be >= 70%, got {confidence}%");
        }

        [TestMethod]
        public void DetectCategory_EventProject_ReturnsEventWithHighConfidence()
        {
            // Arrange
            var description = "Organize a conference and expo event with multiple sessions and networking opportunities";

            // Act
            var (category, confidence) = CategoryDetectorAgent.DetectCategory(description);

            // Assert
            Assert.AreEqual("Event", category, "Category should be detected as Event");
            Assert.IsTrue(confidence >= 70, $"Confidence should be >= 70%, got {confidence}%");
        }

        [TestMethod]
        public void DetectCategory_CaseInsensitive_ReturnsCorrectCategory()
        {
            // Arrange
            var description = "INSTALL SOLAR PANELS AND PHOTOVOLTAIC INVERTER SYSTEMS";

            // Act
            var (category, confidence) = CategoryDetectorAgent.DetectCategory(description);

            // Assert
            Assert.AreEqual("Solar", category, "Detection should be case-insensitive");
            Assert.IsTrue(confidence >= 70, $"Confidence should be >= 70%, got {confidence}%");
        }
    }
}
