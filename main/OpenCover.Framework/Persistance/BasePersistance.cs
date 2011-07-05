﻿using System.Collections.Generic;
using System.Linq;
using OpenCover.Framework.Model;

namespace OpenCover.Framework.Persistance
{
    
    public class BasePersistance : IPersistance
    {
        public BasePersistance()
        {
            CoverageSession = new CoverageSession();
        }

        public CoverageSession CoverageSession { get; private set; }

        public void PersistModule(Module module)
        {
            var list = new List<Module>(CoverageSession.Modules ?? new Module[0]) { module };
            CoverageSession.Modules = list.ToArray();
        }

        public bool IsTracking(string moduleName)
        {
            if (CoverageSession.Modules == null) return false;
            return CoverageSession.Modules.Any(x => x.FullName == moduleName);
        }

        public virtual void Commit()
        {
        }

        public bool GetSequencePointsForFunction(string moduleName, int functionToken, out SequencePoint[] sequencePoints)
        {
            sequencePoints = new SequencePoint[0];
            Class @class;
            var method = GetMethod(moduleName, functionToken, out @class);
            if (method !=null)
            {
                sequencePoints = method.SequencePoints.ToArray();
                return true;
            }
            return false;      
        }

        private Method GetMethod(string moduleName, int functionToken, out Class @class)
        {
            @class = null;
            //c = null;
            if (CoverageSession.Modules == null) return null;
            var module = CoverageSession.Modules.Where(x => x.FullName == moduleName).FirstOrDefault();
            if (module == null) return null;
            foreach (var c in module.Classes)
            {
                @class = c;
                foreach (var method in c.Methods)
                {
                    if (method.MetadataToken == functionToken) return method;
                }
            }
            @class = null;
            return null;
            //return module.Classes
            //    .SelectMany(@class => @class.Methods.Where(method => method.MetadataToken == functionToken))
            //    .FirstOrDefault(method => method != null);
        }

        public string GetClassFullName(string moduleName, int functionToken)
        {
            Class @class;
            GetMethod(moduleName, functionToken, out @class);
            return @class != null ? @class.FullName : null;
        }

        public void SaveVisitPoints(VisitPoint[] visitPoints)
        {
            var summary = from point in visitPoints
                          group point by point.UniqueId into counts
                          let count = counts.Count()
                          select new { point = counts.Key, Count = count };

            foreach (var sum in summary)
            {
                var sum1 = sum;
                foreach (var sequencePoint in from module in CoverageSession.Modules
                                              from @class in module.Classes
                                              from method in @class.Methods
                                              from sequencePoint in method.SequencePoints
                                              where sequencePoint.UniqueSequencePoint == sum1.point
                                              select sequencePoint)
                {
                    sequencePoint.VisitCount += sum.Count;
                }
            }
        }
    }
}