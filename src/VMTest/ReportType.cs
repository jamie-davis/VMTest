namespace VMTest
{
    /// <summary>
    /// Settings that allow the user to specify the type of report used to display a VM's state.
    /// </summary>
    public enum ReportType
    {
        /// <summary>
        /// Use the default for the context
        /// </summary>
        Default, 

        /// <summary>
        /// Display the VM state as a property list
        /// </summary>
        PropertyList,

        /// <summary>
        /// Display the VM state as a single row table
        /// </summary>
        Table,

        /// <summary>
        /// Do not display the VM's state
        /// </summary>
        NoReport
    }
}