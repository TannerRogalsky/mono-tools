using System;
using System.Globalization;
using System.Linq;

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

      if (methodNames.Contains(method.Name)) {
        if (method.HasBody == false) {
          return RuleResult.DoesNotApply;
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
          }
        }
      }

      return Runner.CurrentRuleResult;
    }

  }
}
