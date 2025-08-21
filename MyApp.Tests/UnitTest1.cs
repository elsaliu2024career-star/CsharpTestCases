using NUnit.Framework;
using MyApp.Core;

Console.WriteLine(typeof(Assert).FullName);


namespace MyApp.Tests
{
    [TestFixture]
    public class MathServiceTests
    {
        private MathService _service;

        [SetUp]
        public void Setup()
        {
            _service = new MathService();
        }

        [Test]
        public void Multiply_TwoPositiveNumbers_ReturnsCorrectResult()
        {
            //

            Assert.That(_service.Multiply(2, 3), Is.EqualTo(6));
            Console.WriteLine(typeof(Assert).FullName);

        }

        [Test]
        public void Divide_ByNonZero_ReturnsCorrectResult()
        {
            NUnit.Framework.Assert.That(_service.Divide(10, 2), Is.EqualTo(5));
        }

        [Test]
        public void Divide_ByZero_ThrowsException()
        {
            NUnit.Framework.Assert.Throws<System.DivideByZeroException>(() => _service.Divide(10, 0));
        }
    }
}
