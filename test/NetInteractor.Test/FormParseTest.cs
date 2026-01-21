using System;
using System.IO;
using Xunit;
using NetInteractor;

namespace NetInteractor.Test
{
    public class FormParseTest
    {
        private string GetHtmlFromFile(string fileName)
        {
            return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Html", fileName));
        }

        [Fact]
        public void TestFormCount()
        {
            var html = GetHtmlFromFile("FormCountTest.html");
            var page = new PageInfo("x", html);
            Assert.Equal(3, page.Forms.Length);
        }

        [Fact]
        public void TestFormAttributes()
        {
            var html = GetHtmlFromFile("FormAttributesTest.html");
            var page = new PageInfo("x", html);
            Assert.NotEmpty(page.Forms);
            var form = page.Forms[0];

            Assert.Equal("myForm", form.Name);
            Assert.Equal("myFormID", form.ClientID);
            Assert.Equal("12345", form.Action);
        }

        [Fact]
        public void TestFormValues()
        {
            var html = GetHtmlFromFile("FormValuesTest.html");
            var page = new PageInfo("x", html);
            Assert.NotEmpty(page.Forms);
            var form = page.Forms[0];

            Assert.NotEmpty(form.FormValues);
            Assert.Equal("Fish Oil", form.FormValues["itemName"]);
            Assert.Equal("2", form.FormValues["sex"]);
            Assert.Equal("Honda", form.FormValues["make"]);
        }
    }
}
