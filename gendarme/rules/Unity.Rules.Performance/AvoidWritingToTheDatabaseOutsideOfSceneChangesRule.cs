using System;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;

using Gendarme.Framework;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Unity.Rules.Performance {
  [Problem("Writing to the database inside of a frame causes a heavy, blocking IO operation that will result in frame skips.")]
  [Solution("Only write to the database inside of a scene change.")]
  public class AvoidWritingToTheDatabaseOutsideOfSceneChangesRule : Rule, IMethodRule {

    private readonly string[] methodNames = {
                                              "Update",
                                              "LateUpdate",
                                              "FixedUpdate"
                                            };

    public RuleResult CheckMethod(MethodDefinition method) {
      if (!Utilities.IsMonoBehaviour(method.DeclaringType)) {
        return RuleResult.DoesNotApply;
      }

      if (method.HasBody == false) {
        return RuleResult.DoesNotApply;
      }

      if (methodNames.Contains(method.Name)) {
        HashSet<MethodDefinition> seenMethods = new HashSet<MethodDefinition>();
        this.RecursiveCheckMethod(seenMethods, method);
      } else {
        return RuleResult.DoesNotApply;
      }
      return Runner.CurrentRuleResult;
    }

    public RuleResult RecursiveCheckMethod(HashSet<MethodDefinition> seenMethods, MethodDefinition method) {
      if (method == null || seenMethods.Contains(method)){
        return Runner.CurrentRuleResult;
      } else {
        seenMethods.Add(method);
      }

      if (method.HasBody == false) {
        return Runner.CurrentRuleResult;
      }

      foreach(Instruction instruction in method.Body.Instructions) {
        Code code = instruction.OpCode.Code;
        if (code != Code.Call) {
          continue;
        }

        MethodReference def = instruction.Operand as MethodReference;
        if (def == null) {
          continue;
        }

        if (def.DeclaringType.Name == "LQDB") {
          string message = String.Format(CultureInfo.CurrentCulture, "Don't hit LQDB in an update loop.");
          Runner.Report( method, instruction, Severity.Critical, Confidence.Total, message );

          continue;
        } else {
          this.RecursiveCheckMethod(seenMethods, def.Resolve());
        }
      }

      return Runner.CurrentRuleResult;
    }

  }
}
