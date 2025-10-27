using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlsCdkApp
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            new BlsCdkAppStack(app, "BlsCdkAppStack", new StackProps());
            app.Synth();
        }
    }
}
