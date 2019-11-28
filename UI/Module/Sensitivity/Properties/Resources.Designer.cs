﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Sensitivity.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Sensitivity.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to sensitivity::tell(rvis_sensitivity_design, rvis_sensitivity_out)
        ///rvis_sensitivity_ignored &lt;- capture.output(rvis_sensitivity_printed &lt;- print(rvis_sensitivity_design))
        ///rvis_sensitivity_measures &lt;- cbind(rvis_sensitivity_printed, rvis_sensitivity_design$V, rvis_sensitivity_design$D1, rvis_sensitivity_design$Dt)
        ///colnames(rvis_sensitivity_measures) &lt;- c(&quot;first&quot;, &quot;total&quot;, &quot;V&quot;, &quot;D1&quot;, &quot;Dt&quot;).
        /// </summary>
        internal static string FAST99_TELL_AND_COLLECT {
            get {
                return ResourceManager.GetString("FAST99_TELL_AND_COLLECT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to rvis_sensitivity_sampleSize &lt;- {0}
        ///rvis_sensitivity_factors &lt;- c({1})
        ///rvis_sensitivity_omegaFromCukier &lt;- {2}
        ///rvis_sensitivity_qfs &lt;- c({3})
        ///rvis_sensitivity_qfargs &lt;- list({4})
        ///
        ///rvis_sensitivity_nFactors &lt;- length(rvis_sensitivity_factors)
        ///rvis_sensitivity_omega &lt;- NULL
        ///
        ///if (rvis_sensitivity_omegaFromCukier)
        ///{{
        ///  rvis_sensitivity_omega &lt;- fast::freq_cukier(rvis_sensitivity_nFactors)
        ///  rvis_sensitivity_maxOmega &lt;- max(rvis_sensitivity_omega)
        ///  rvis_sensitivity_sampleSize &lt;- rvis_sensitivity_max [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string FMT_CREATE_FAST99_DESIGN {
            get {
                return ResourceManager.GetString("FMT_CREATE_FAST99_DESIGN", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to rvis_sensitivity_factors &lt;- c({0})
        ///rvis_sensitivity_r &lt;- {1}
        ///rvis_sensitivity_binf &lt;- c({2}) 
        ///rvis_sensitivity_bsup &lt;- c({3}) 
        ///rvis_sensitivity_levels &lt;- rep(as.integer({4}), length(rvis_sensitivity_factors))
        ///rvis_sensitivity_grid_jump &lt;- rep(as.integer({5}), length(rvis_sensitivity_factors))
        ///  
        ///rvis_sensitivity_design_{6:00000000} &lt;- sensitivity::morris(
        ///  factors = rvis_sensitivity_factors, 
        ///  r = rvis_sensitivity_r,
        ///  binf = rvis_sensitivity_binf,
        ///  bsup = rvis_sensitivity_bsup,
        ///  design = li [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string FMT_CREATE_MORRIS_DESIGN {
            get {
                return ResourceManager.GetString("FMT_CREATE_MORRIS_DESIGN", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to rvis_sensitivity_out_{0:00000000} &lt;- c({1})
        ///sensitivity::tell(rvis_sensitivity_design_{0:00000000}, rvis_sensitivity_out_{0:00000000})
        ///rvis_ignored &lt;- capture.output(rvis_p_{0:00000000} &lt;- print(rvis_sensitivity_design_{0:00000000})).
        /// </summary>
        internal static string FMT_MORRIS_TELL_AND_COLLECT {
            get {
                return ResourceManager.GetString("FMT_MORRIS_TELL_AND_COLLECT", resourceCulture);
            }
        }
    }
}
