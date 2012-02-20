using System;
using System.Linq;
using Nancy;
using Sample.Nancy.Models;

namespace Sample.Nancy
{
    public class MainModule : NancyModule
    {
        public MainModule()
        {
            Get["/nancy"] = x =>
            {
                var model = new Index() {Name = "Boss Hawg"};

                return View["Index", model];
            };

            Post["/nancy"] = x =>
            {
                var model = new Index() {Name = "Boss Hawg"};

                model.Posted = this.Request.Form.posted.HasValue ? (string)this.Request.Form.posted.Value : "Nothing :-(";

                if (model.Posted == "crash")
                {
                    throw new ApplicationException("Boom!");
                }

                return View["Index", model];
            };

            Get["/fileupload"] = x =>
            {
                var model = new Index() {Name = "Boss Hawg"};

                return View["FileUpload", model];
            };

            Post["/fileupload"] = x =>
            {
                var model = new Index() {Name = "Boss Hawg"};

                var file = this.Request.Files.FirstOrDefault();
                string fileDetails = "None";

                if (file != null)
                {
                    fileDetails = string.Format("{0} ({1}) {2}bytes", file.Name, file.ContentType, file.Value.Length);
                }

                model.Posted = fileDetails;

                return View["FileUpload", model];
            };
        }
    }
}