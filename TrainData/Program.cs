using System.Collections.Generic;

using Arcanist.OpenLDBWS;

namespace Arcanist
{
    using System;
    using System.Linq;
    
    internal class Program
    {
        public static void Main(string[] args)
        {
            var original = Console.ForegroundColor;
            Run(args.ToList());
            Console.ForegroundColor = original;
        }
        
        public static void Run(List<string> arguments)
        {
            // get your own token for free from http://realtime.nationalrail.co.uk/OpenLDBWSRegistration/
            // tokens are limited to 5 million requests every 4 weeks
            var token = "00000000-0000-0000-0000-000000000000";
            
            var clientAccessToken = new AccessToken {TokenValue = token};

            // default departure and destination
            var origin = "HYM";
            var destination = "LVG";

            if (arguments.Count == 3)
            {
                origin = arguments[1];
                destination = arguments[2];
            }
            else if (arguments.Count == 2)
            {
                destination = arguments[1];
            }

            var client = new LDBServiceSoapClient();
            
            var request = new GetDepBoardWithDetailsRequest();
            request.AccessToken = clientAccessToken;
            request.crs = origin;
            request.numRows = 50;
            request.filterCrs = destination;
            request.filterType = FilterType.to;
            var response = client.GetDepBoardWithDetails(request);
            
            var stationBoard = response.GetStationBoardResult;
            
            if (!stationBoard.areServicesAvailable)
            {
                Output.Write("No services available for the requested station").End();
                return;
            }


            Output.Write("Services from ")._(stationBoard.locationName, ConsoleColor.White)._(" to ")
                ._(stationBoard.filterLocationName, ConsoleColor.White).End();
            Output.BlankLine();

            if (stationBoard.nrccMessages != null)
            {
                foreach (var message in stationBoard.nrccMessages)
                {
                    Output.Write(message.Value, ConsoleColor.Yellow).End();
                    Output.BlankLine();
                }
            }

            IEnumerable<ServiceItemWithCallingPoints1> services = new List<ServiceItemWithCallingPoints1>();
            if (stationBoard.trainServices != null)
            {
                services = services.Union(stationBoard.trainServices);
            }

            if (stationBoard.busServices != null)
            {
                services = services.Union(stationBoard.busServices.Select(x =>
                {
                    x.platform = "BUS";
                    return x;
                }));
            }

            services = services.OrderBy(x => x.std).ToList();


            var maxLengthDestination = services.Max(x =>
            {
                var serviceLocation = x.destination.First();
                var length = serviceLocation.locationName.Length;
                if (!string.IsNullOrWhiteSpace(serviceLocation.via))
                {
                    length += serviceLocation.via.Length + 1;
                }

                return length;
            });
            
            Output.Write("STD  ")._("  ")._("Pl.")._("  ")._("Destination".PadRight(maxLengthDestination))._("  ")._("ETD").End();

            foreach (var service in services)
            {
                var servDest = service.destination.First();

                var thisDestLength = servDest.locationName.Length +
                                     (string.IsNullOrWhiteSpace(servDest.via) ? 0 : servDest.via.Length + 1);

                var destLine = Output.Write(service.std, ConsoleColor.White)._("  ")
                    ._((service.platform == null ? string.Empty : service.platform).PadRight(3),
                        service.platform == "BUS" ? ConsoleColor.Red : ConsoleColor.DarkGray)._("  ")
                    ._(servDest.locationName, ConsoleColor.White);
                if (!string.IsNullOrWhiteSpace(servDest.via))
                {
                    destLine._(" ")._(servDest.via, ConsoleColor.DarkGray);
                }

                destLine._(string.Empty.PadLeft(maxLengthDestination - thisDestLength));

                destLine._("  ")._(service.etd,
                        service.etd == "On time"
                            ? ConsoleColor.Gray
                            : (service.etd == "Cancelled" ? ConsoleColor.Red : ConsoleColor.Yellow))
                    .End();

                if (service.isCancelled)
                {
                    Output.Indent(1)._("CANCELLED: ", ConsoleColor.Red)._(service.cancelReason, ConsoleColor.Red).End();
                }

                if (!string.IsNullOrWhiteSpace(service.delayReason))
                {
                    Output.Indent(1)._("Delayed: ", ConsoleColor.Yellow)._(service.cancelReason, ConsoleColor.Yellow)
                        .End();
                }

                if (service.adhocAlerts != null)
                {
                    foreach (var adhocAlert in service.adhocAlerts)
                    {
                        Output.Indent(1)._("Note: ")._(adhocAlert).End();
                    }
                }


                var callingPoints = service.subsequentCallingPoints.First().callingPoint;
                var callingat = Output.Indent(1)._("Calling at: ", ConsoleColor.DarkGray);
                foreach (var cp in callingPoints)
                {
                    if (cp.crs == destination || cp.crs == servDest.crs)
                    {
                        callingat._(cp.locationName, ConsoleColor.DarkGray)._(" (expected ", ConsoleColor.DarkGray)
                            ._(cp.et == "On time" ? cp.st : cp.et)._(")", ConsoleColor.DarkGray).End();
                        break;
                    }

                    callingat._(cp.locationName, ConsoleColor.DarkGray)._(", ", ConsoleColor.DarkGray);
                }

                if (!string.IsNullOrWhiteSpace(service.@operator))
                {
                    Output.Indent(1)._("Operated by ", ConsoleColor.DarkGray)
                        ._(service.@operator, ConsoleColor.DarkGray).End();
                }

                var serviceLength = service.length;
                if (serviceLength != 0)
                {
                    Output.Indent(1)._("This train has ", ConsoleColor.DarkGray)
                        ._(serviceLength.ToString(), ConsoleColor.DarkGray)._(" carriages.", ConsoleColor.DarkGray)
                        .End();
                }

                Output.BlankLine();
            }
            
            // required by API licence terms :|
            Output.Write("Powered by National Rail Enquiries", ConsoleColor.DarkGray);
            Output.BlankLine();
        }

    }
}