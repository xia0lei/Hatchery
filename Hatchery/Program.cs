using System;
using Hatchery.Framework;
using Hatchery.Framework.Utility;
using Hatchery.Test;

namespace Hatchery
{
	class Program
	{
		static void Main(string[] args)
		{
			string inputMode = args[0];
			int mode = 0;

			if (inputMode == "TestCases")
			{
				mode = 1;
			}
			else if (inputMode == "Hatchery")
			{
				mode = 2;
			}
			else
			{
				return;
			}

			switch (mode)
			{
				case 1:
					{
						string caseName = args[1];
						TestCases testCases = new TestCases();
						testCases.Run(caseName);
					}
					break;
				case 2:
					{
						string bootService = args[1];
						string bootPath = args[2];
						string bootServiceName = "";
						if (args.Length == 4)
						{
							bootServiceName = args[3];
						}
						BootServices startFunc = delegate ()
						{
							HatcheryUtility.NewService(bootService, bootServiceName);
						};
						Server server = new Server();
						server.Run(bootPath, startFunc);
					}
					break;
				default:
					LoggerHelper.Info(0, String.Format("Mode:{0} not supported", mode));
					break;
			}
		}
	}
}
