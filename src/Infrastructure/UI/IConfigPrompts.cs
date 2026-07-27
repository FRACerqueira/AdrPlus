// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;

namespace AdrPlus.Infrastructure.UI
{
    /// <summary>
    /// Prompts used exclusively by the <c>config</c> command: editing the application
    /// and repository configuration files, and defining the migration pattern used by
    /// <c>config --migrate</c>.
    /// </summary>
    internal interface IConfigPrompts
    {
        /// <summary>
        /// Clears the console history related to migration operations, ensuring that any previous migration logs or messages are removed from the console output. This method is typically used to maintain a clean and organized console display during migration processes, allowing users to focus on current migration activities without being distracted by past logs.
        /// </summary>
        void ClearHistoryMigration();

        /// <summary>
        /// Prompts the user to select a Title position for a file.
        /// </summary>
        /// <param name="filename">The name of the file.</param>
        /// <param name="maxValue">The maximum position value allowed.</param>
        /// <param name="defaultValue">The default position value.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A tuple containing whether the operation was aborted and the selected position value.</returns>
        (bool IsAborted, int Value) PromptSelectTitlePosition(string filename, int maxValue, int defaultValue, CancellationToken cancellationToken);

        /// <summary>
        /// Prompts the user to select a prefix position for a file.
        /// </summary>
        /// <param name="filename">The name of the file.</param>
        /// <param name="maxValue">The maximum position value allowed.</param>
        /// <param name="defaultValue">The default position value.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A tuple containing whether the operation was aborted and the selected position value.</returns>
        (bool IsAborted, int Value) PromptSelectPrefixPosition(string filename, int maxValue, int defaultValue, CancellationToken cancellationToken);

        /// <summary>
        /// Prompts the user to select a revision length from a file at a specified position.
        /// </summary>
        /// <param name="filename">The name or path of the file.</param>
        /// <param name="position">The position within the file.</param>
        /// <param name="maxValue">The maximum allowed value for the revision length.</param>
        /// <param name="defaultValue">The default value to use if no selection is made.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing whether the operation was aborted and the selected value.</returns>
        (bool IsAborted, int Value) PromptSelectRevisionLength(string filename, int position, int maxValue, int defaultValue, CancellationToken cancellationToken);

        /// <summary>
        /// Prompts the user to select a Revision position for a file.
        /// </summary>
        /// <param name="filename">The name of the file.</param>
        /// <param name="maxValue">The maximum position value allowed.</param>
        /// <param name="defaultValue">The default position value.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A tuple containing whether the operation was aborted and the selected position value.</returns>
        (bool IsAborted, int Value) PromptSelectRevisionPosition(string filename, int maxValue, int defaultValue, CancellationToken cancellationToken);

        /// <summary>
        /// Prompts the user to select a version length from a file at a specified position.
        /// </summary>
        /// <param name="filename">The name or path of the file.</param>
        /// <param name="position">The position within the file.</param>
        /// <param name="maxValue">The maximum allowed value for the version length.</param>
        /// <param name="defaultValue">The default value to use if no selection is made.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing whether the operation was aborted and the selected value.</returns>
        (bool IsAborted, int Value) PromptSelectVersionLength(string filename, int position, int maxValue, int defaultValue, CancellationToken cancellationToken);

        /// <summary>
        /// Prompts the user to select a Version position for a file.
        /// </summary>
        /// <param name="filename">The name of the file.</param>
        /// <param name="maxValue">The maximum position value allowed.</param>
        /// <param name="defaultValue">The default position value.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A tuple containing whether the operation was aborted and the selected position value.</returns>
        (bool IsAborted, int Value) PromptSelectVersionPosition(string filename, int maxValue, int defaultValue, CancellationToken cancellationToken);

        /// <summary>
        /// Prompts the user to select a prefix length from a file at a specified position.
        /// </summary>
        /// <param name="filename">The name or path of the file.</param>
        /// <param name="position">The position within the file.</param>
        /// <param name="maxValue">The maximum allowed value for the prefix length.</param>
        /// <param name="defaultValue">The default value to use if no selection is made.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing whether the operation was aborted, the selected value, and the prefix value string.</returns>
        (bool IsAborted, int Value, string PrefixValue) PromptSelectPrefixLength(string filename, int position, int maxValue, int defaultValue, CancellationToken cancellationToken);

        /// <summary>
        /// Prompts the user to select a number position for a file.
        /// </summary>
        /// <param name="filename">The name of the file.</param>
        /// <param name="maxValue">The maximum position value allowed.</param>
        /// <param name="defaultValue">The default position value.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A tuple containing whether the operation was aborted and the selected position value.</returns>
        (bool IsAborted, int Value) PromptSelectNumberPosition(string filename, int maxValue, int defaultValue, CancellationToken cancellationToken);

        /// <summary>
        /// Prompts the user to select a number length for a file at a specified position.
        /// </summary>
        /// <param name="filename">The name of the file.</param>
        /// <param name="position">The position within the file.</param>
        /// <param name="maxValue">The maximum allowed value for the number length.</param>
        /// <param name="defaultValue">The default value to use if no selection is made.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing whether the operation was aborted and the selected number length value.</returns>
        (bool IsAborted, int Value) PromptSelectNumberLength(string filename, int position, int maxValue, int defaultValue, CancellationToken cancellationToken);

        /// <summary>
        /// Prompts the user to provide existing fields from a filename.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the prompt operation.</param>
        /// <returns>A tuple containing a flag indicating whether the operation was aborted and an array of field values from the filename.</returns>
        (bool IsAborted, string[] FieldsFromFileAdr) PromptFieldsFromFileAdr(CancellationToken cancellationToken);

        /// <summary>
        /// Prompts the user for sample file migration.
        /// </summary>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a flag indicating whether the operation was aborted and the sample file migration result.</returns>
        (bool IsAborted, string SampleFileMigration) PromptSampleFileMigration(CancellationToken cancellationToken);

        /// <summary>
        /// Prompts the user to select an option from a list of fields.
        /// </summary>
        /// <param name="defaultvalue">The default value to select.</param>
        /// <param name="fields">The list of fields to choose from.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple indicating whether the operation was aborted and the user's selected field.</returns>
        (bool IsAborted, FieldsJson? Content) PromptConfigJsonRepoSelect(FieldsJson defaultvalue, IEnumerable<FieldsJson> fields, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to select an option from a list of fields.
        /// </summary>
        /// <param name="defaultvalue">The default value to select.</param>
        /// <param name="fields">The list of fields to choose from.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple indicating whether the operation was aborted and the user's selected field.</returns>
        (bool IsAborted, FieldsJson? Content) PromptConfigJsonAppSelect(FieldsJson defaultvalue, IEnumerable<FieldsJson> fields, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit a field prefix and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered content.</returns>
        (bool IsAborted, string Content) PromptEditFieldPrefix(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit a field length sequence and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson"> The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered content.</returns>
        (bool IsAborted, int Content) PromptEditFieldLenSeq(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit a field language and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered language content.</returns>
        (bool IsAborted, string Content) PromptEditFieldLanguage(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the behavior when no arguments are provided and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered behavior content.</returns>
        (bool IsAborted, string Content) PromptEditFieldBehaviorWithoutArgs(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the repository folder path and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered folder path.</returns>
        (bool IsAborted, string Content) PromptEditFieldFolderRepo(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the command to open ADR files and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered command content.</returns>
        (bool IsAborted, string Content) PromptEditFielOpenAdr(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit version field length and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered field length.</returns>
        (bool IsAborted, int Content) PromptEditFieldVersion(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit revision field length and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered field length.</returns>
        (bool IsAborted, int Content) PromptEditFieldRevision(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the available scopes and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered scopes content.</returns>
        (bool IsAborted, string Content) PromptEditFieldScopes(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the skip detail field and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="fields">The available field options to choose from.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered skip detail content.</returns>
        (bool IsAborted, string Content) PromptEditFieldskipdomain(FieldsJson fieldsJson, IEnumerable<FieldsJson> fields, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the scope field length and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered scope length.</returns>
        (bool IsAborted, int Content) PromptEditFieldLenScope(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the folder-by-scope setting and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the selected boolean value.</returns>
        (bool IsAborted, bool Content) PromptEditFieldFolderByScope(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the case transformation format and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered case format.</returns>
        (bool IsAborted, string Content) PromptEditFieldCaseTransform(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the field separator character and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered separator.</returns>
        (bool IsAborted, string Content) PromptEditFieldSeparator(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit a status field value and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered status.</returns>
        (bool IsAborted, string Content) PromptEditFieldStatus(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the header  text and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="maxlength">Max length of text</param>
        /// <param name="sugestion">Sugestion to text</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered disclaimer text.</returns>
        (bool IsAborted, string Content) PromptEditFieldHeaderText(FieldsJson fieldsJson, int maxlength, string sugestion, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to select an ADR template file for configuration.
        /// </summary>
        /// <param name="root">The root directory where template discovery starts.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A tuple containing a boolean indicating if the operation was aborted and the file path of the selected ADR template. If the operation was aborted, the file path will be null or empty.
        /// </returns>
        (bool IsAborted, string FilePathAdrTemplate) PromptConfigTemplateAdrSelect(string root, CancellationToken cancellationToken = default);
    }
}
