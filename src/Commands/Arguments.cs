// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Commands
{
    /// <summary>
    /// Enumeration of command-line arguments supported by AdrPlus commands.
    /// </summary>
    internal enum Arguments
    {
        [CommandArgument("-w", "--wizard")]
        [HelpUsage(UsageArgumments.Wizard, "HelpUsageWizardNew")]
        WizardNew,
        [CommandArgument("-w", "--wizard")]
        [HelpUsage(UsageArgumments.Wizard, "HelpUsageWizardNewVer")]
        WizardVersion,
        [CommandArgument("-w", "--wizard")]
        [HelpUsage(UsageArgumments.Wizard, "HelpUsageWizardNewRev")]
        WizardReview,
        [CommandArgument("-w", "--wizard")]
        [HelpUsage(UsageArgumments.Wizard, "HelpUsageWizardSupersede")]
        WizardSupersede,
        [CommandArgument("-w", "--wizard")]
        [HelpUsage(UsageArgumments.Wizard, "HelpUsageWizardApprove")]
        WizardApprove,
        [CommandArgument("-w", "--wizard")]
        [HelpUsage(UsageArgumments.Wizard, "HelpUsageWizardReject")]
        WizardReject,
        [CommandArgument("-w", "--wizard")]
        [HelpUsage(UsageArgumments.Wizard, "HelpUsageWizardUndoStatus")]
        WizardUndoStatus,
        [CommandArgument("-w", "--wizard")]
        [HelpUsage(UsageArgumments.Wizard, "HelpUsageWizardInit")]
        WizardInit,
        [CommandArgument("-a", "--application")]
        [HelpUsage(UsageArgumments.Wizard, "HelpUsageWizardConfigApp")]
        WizardConfigApplication,
        [CommandArgument("-t", "--template")]
        [HelpUsage(UsageArgumments.Wizard, "HelpUsageWizardConfigTemplate")]
        WizardConfigTemplate,
        [CommandArgument("-r", "--repository")]
        [HelpUsage(UsageArgumments.Wizard, "HelpUsageWizardConfigRepo")]
        WizardConfigRepository,
        [CommandArgument("-f", "--file")]
        [HelpUsage(UsageArgumments.OptionalWithValue, "HelpUsageFileConfig")]
        FileConfig,
        [CommandArgument("-f", "--file")]
        [HelpUsage(UsageArgumments.OptionalWithValueWhenWizard, "HelpUsageFileAdr")]
        FileAdr,
        [CommandArgument("-s", "--sequence")]
        [HelpUsage(UsageArgumments.OptionalWithValueWhenWizard, "HelpUsageSequenceAdr")]
        SequenceAdr,
        [CommandArgument("-p", "--path")]
        [HelpUsage(UsageArgumments.OptionalWithValueWhenWizard, "HelpUsageTargetRepoPath")]
        TargetRepo,
        [CommandArgument("-t", "--title")]
        [HelpUsage(UsageArgumments.OptionalWithValueWhenWizard, "HelpUsageTitleAdr")]
        TitleAdr,
        [CommandArgument("-d", "--domain")]
        [HelpUsage(UsageArgumments.OptionalWithValueWhenWizard, "HelpUsageDomainAdr")]
        DomainAdr,
        [CommandArgument("-s", "--scope")]
        [HelpUsage(UsageArgumments.OptionalWithValueWhenWizard, "HelpUsageScopeAdr")]
        ScopeAdr,
        [CommandArgument("-o", "--open")]
        [HelpUsage(UsageArgumments.Optional, "HelpUsageOpenAdr")]
        OpenAdr,
        [CommandArgument("-r", "--refdate")]
        [HelpUsage(UsageArgumments.Optional, "HelpUsageDateRefAdr")]
        DateRefAdr,
        [CommandArgument("-h", "--help")]
        [HelpUsage(UsageArgumments.Optional, "HelpUsageHelp")]
        Help
    }
}
