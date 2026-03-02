#region Usings declarations

using System.Reflection;

using DiagnosableExceptions.GenDoc;
using DiagnosableExceptions.Usage.Infrastructure.Adapters;
using DiagnosableExceptions.Usage.Model;

using NFluent;

#endregion

namespace DiagnosableExceptions.UnitTests {

    public class AssemblyErrorDocumentationReaderTests {

        [Fact]
        public void Test1() {
            // Setup
            Assembly assembly = Assembly.GetAssembly(typeof(Temperature))!;

            // Exercise
            IEnumerable<ErrorDocumentation> documentation = AssemblyErrorDocumentationReader.GetErrorDocumentationFrom(assembly);

            // Verify
            ErrorDocumentation[] errorDocumentations = documentation.ToArray();

            Check.That(errorDocumentations).CountIs(4);
            ErrorDocumentation amountCurrencyMismatch                          = errorDocumentations[0];
            ErrorDocumentation bankTransactionFileDateOutOfStatementPeriod     = errorDocumentations[1];
            ErrorDocumentation bankTransactionFileStatementTotalAmountMismatch = errorDocumentations[2];
            ErrorDocumentation temperatureBelowAbsoluteZero                    = errorDocumentations[3];

            // ------------------------------------------------------------------
            // AMOUNT_CURRENCY_MISMATCH
            // ------------------------------------------------------------------

            Check.That(amountCurrencyMismatch.Code).IsEqualTo("AMOUNT_CURRENCY_MISMATCH");
            Check.That(amountCurrencyMismatch.Title).IsEqualTo("Amount currency mismatch");
            Check.That(amountCurrencyMismatch.Explanation).IsEqualTo("This error occurs when trying to use multiple amounts together in an operation while they are expressed in different currencies.");
            Check.That(amountCurrencyMismatch.BusinessRule).IsEqualTo("All monetary operations must involve amounts expressed in the same currency.");

            Check.That(amountCurrencyMismatch.Exception).IsEqualTo(typeof(InvalidAmountOperationException));
            Check.That(amountCurrencyMismatch.ErrorSource).IsEqualTo(typeof(Amount));
            Check.That(amountCurrencyMismatch.FactoryMethodName).IsEqualTo("CurrencyMismatch");

            Check.That(amountCurrencyMismatch.Diagnostics).CountIs(2);

            Check.That(amountCurrencyMismatch.Diagnostics[0].Cause).IsEqualTo("Amounts were used in a monetary operation without having been converted to the same currency.");
            Check.That(amountCurrencyMismatch.Diagnostics[0].Type).IsEqualTo(ErrorCauseType.System);
            Check.That(amountCurrencyMismatch.Diagnostics[0].AnalysisLead).IsEqualTo("Verify whether all amounts involved in the operation were converted to a common currency before being used together.");

            Check.That(amountCurrencyMismatch.Diagnostics[1].Cause).IsEqualTo("Amounts expected to be expressed in the same currency were provided with different currencies.");
            Check.That(amountCurrencyMismatch.Diagnostics[1].Type).IsEqualTo(ErrorCauseType.SystemOrInput);
            Check.That(amountCurrencyMismatch.Diagnostics[1].AnalysisLead).IsEqualTo("Check the currencies associated with each amount and confirm whether a common currency was expected for this operation.");

            Check.That(amountCurrencyMismatch.Examples).CountIs(1);

            Check.That(amountCurrencyMismatch.Examples[0].DetailedMessage).IsNotNull();
            Check.That(amountCurrencyMismatch.Examples[0].DetailedMessage).IsEqualTo("Failed to perform the monetary operation because the involved amounts are expressed in different currencies: 127.33 EUR and 57689 USD.");
            Check.That(amountCurrencyMismatch.Examples[0].ShortMessage).IsEqualTo("Currency mismatch");

            // ------------------------------------------------------------------
            // BANK_TRANSACTION_FILE_DATE_OUT_OF_STATEMENT_PERIOD
            // ------------------------------------------------------------------

            Check.That(bankTransactionFileDateOutOfStatementPeriod.Code).IsEqualTo("BANK_TRANSACTION_FILE_DATE_OUT_OF_STATEMENT_PERIOD");
            Check.That(bankTransactionFileDateOutOfStatementPeriod.Title).IsEqualTo("Transaction date outside statement period");
            Check.That(bankTransactionFileDateOutOfStatementPeriod.Explanation).IsEqualTo("This error occurs when trying to validate a bank statement file that contains one or more transactions dated outside the statement period.");
            Check.That(bankTransactionFileDateOutOfStatementPeriod.BusinessRule).IsEqualTo("All transactions must occur within the statement period.");

            Check.That(bankTransactionFileDateOutOfStatementPeriod.Exception).IsEqualTo(typeof(NonCompliantBankTransactionFileException));
            Check.That(bankTransactionFileDateOutOfStatementPeriod.ErrorSource).IsEqualTo(typeof(BankTransactionFileValidator));
            Check.That(bankTransactionFileDateOutOfStatementPeriod.FactoryMethodName).IsEqualTo("DateOutOfStatementPeriod");

            Check.That(bankTransactionFileDateOutOfStatementPeriod.Diagnostics).CountIs(4);

            Check.That(bankTransactionFileDateOutOfStatementPeriod.Diagnostics[0].Cause).IsEqualTo("The transaction date provided in the statement file is incorrect or inconsistent with the actual transaction date.");
            Check.That(bankTransactionFileDateOutOfStatementPeriod.Diagnostics[0].Type).IsEqualTo(ErrorCauseType.Input);
            Check.That(bankTransactionFileDateOutOfStatementPeriod.Diagnostics[0].AnalysisLead).IsEqualTo("Verify the transaction date present in the input file and confirm its consistency with the actual transaction timeline.");

            Check.That(bankTransactionFileDateOutOfStatementPeriod.Diagnostics[1].Cause).IsEqualTo("The statement period defined in the file does not match the actual coverage period of the transactions.");
            Check.That(bankTransactionFileDateOutOfStatementPeriod.Diagnostics[1].Type).IsEqualTo(ErrorCauseType.Input);
            Check.That(bankTransactionFileDateOutOfStatementPeriod.Diagnostics[1].AnalysisLead).IsEqualTo("Check whether the statement start and end dates in the file align with the period covered by the transactions.");

            Check.That(bankTransactionFileDateOutOfStatementPeriod.Diagnostics[2].Cause).IsEqualTo("The transaction was posted after the statement was generated but was mistakenly included in the file.");
            Check.That(bankTransactionFileDateOutOfStatementPeriod.Diagnostics[2].Type).IsEqualTo(ErrorCauseType.SystemOrInput);
            Check.That(bankTransactionFileDateOutOfStatementPeriod.Diagnostics[2].AnalysisLead).IsEqualTo("Determine whether late-posted transactions were included in the statement generation process.");

            Check.That(bankTransactionFileDateOutOfStatementPeriod.Diagnostics[3].Cause).IsEqualTo("An internal processing error caused the transaction date to be shifted during data transformation or import.");
            Check.That(bankTransactionFileDateOutOfStatementPeriod.Diagnostics[3].Type).IsEqualTo(ErrorCauseType.System);
            Check.That(bankTransactionFileDateOutOfStatementPeriod.Diagnostics[3].AnalysisLead).IsEqualTo("Examine the data import and transformation stages to confirm that transaction dates are preserved without alteration.");

            Check.That(bankTransactionFileDateOutOfStatementPeriod.Examples).CountIs(1);
            Check.That(bankTransactionFileDateOutOfStatementPeriod.Examples[0].DetailedMessage).Contains("Transaction dated");
            Check.That(bankTransactionFileDateOutOfStatementPeriod.Examples[0].ShortMessage).IsEqualTo("Transaction date is outside the statement period.");

            // ------------------------------------------------------------------
            // BANK_TRANSACTION_FILE_STATEMENT_TOTAL_AMOUNT_MISMATCH
            // ------------------------------------------------------------------

            Check.That(bankTransactionFileStatementTotalAmountMismatch.Code).IsEqualTo("BANK_TRANSACTION_FILE_STATEMENT_TOTAL_AMOUNT_MISMATCH");
            Check.That(bankTransactionFileStatementTotalAmountMismatch.Title).IsEqualTo("Statement total amount mismatch");
            Check.That(bankTransactionFileStatementTotalAmountMismatch.Explanation).IsEqualTo("This error occurs when trying to validate a bank statement file whose declared total amount does not match the sum of the individual transaction amounts.");
            Check.That(bankTransactionFileStatementTotalAmountMismatch.BusinessRule).IsEqualTo("The statement total amount must equal the sum of all transaction amounts included in the statement.");

            Check.That(bankTransactionFileStatementTotalAmountMismatch.Exception).IsEqualTo(typeof(NonCompliantBankTransactionFileException));
            Check.That(bankTransactionFileStatementTotalAmountMismatch.ErrorSource).IsEqualTo(typeof(BankTransactionFileValidator));
            Check.That(bankTransactionFileStatementTotalAmountMismatch.FactoryMethodName).IsEqualTo("StatementTotalAmountMismatch");

            Check.That(bankTransactionFileStatementTotalAmountMismatch.Diagnostics).CountIs(4);

            Check.That(bankTransactionFileStatementTotalAmountMismatch.Diagnostics[0].Cause).IsEqualTo("The total amount declared in the statement file does not match the sum of the individual transaction amounts.");
            Check.That(bankTransactionFileStatementTotalAmountMismatch.Diagnostics[0].Type).IsEqualTo(ErrorCauseType.Input);
            Check.That(bankTransactionFileStatementTotalAmountMismatch.Diagnostics[0].AnalysisLead).IsEqualTo("Verify the declared total amount in the file and compare it with the sum of all transaction amounts.");

            Check.That(bankTransactionFileStatementTotalAmountMismatch.Diagnostics[1].Cause).IsEqualTo("One or more transactions are missing or duplicated in the statement file.");
            Check.That(bankTransactionFileStatementTotalAmountMismatch.Diagnostics[1].Type).IsEqualTo(ErrorCauseType.Input);
            Check.That(bankTransactionFileStatementTotalAmountMismatch.Diagnostics[1].AnalysisLead).IsEqualTo("Check whether all expected transactions are present exactly once in the statement file.");

            Check.That(bankTransactionFileStatementTotalAmountMismatch.Diagnostics[2].Cause).IsEqualTo("A rounding or precision error occurred when calculating the statement total amount.");
            Check.That(bankTransactionFileStatementTotalAmountMismatch.Diagnostics[2].Type).IsEqualTo(ErrorCauseType.SystemOrInput);
            Check.That(bankTransactionFileStatementTotalAmountMismatch.Diagnostics[2].AnalysisLead).IsEqualTo("Examine how rounding and precision rules were applied when computing the statement total.");

            Check.That(bankTransactionFileStatementTotalAmountMismatch.Diagnostics[3].Cause).IsEqualTo("An internal processing error altered transaction amounts during file parsing or transformation.");
            Check.That(bankTransactionFileStatementTotalAmountMismatch.Diagnostics[3].Type).IsEqualTo(ErrorCauseType.System);
            Check.That(bankTransactionFileStatementTotalAmountMismatch.Diagnostics[3].AnalysisLead).IsEqualTo("Inspect the file parsing and transformation stages to confirm that transaction amounts remain unchanged.");

            Check.That(bankTransactionFileStatementTotalAmountMismatch.Examples).CountIs(1);
            Check.That(bankTransactionFileStatementTotalAmountMismatch.Examples[0].DetailedMessage).Contains("declared statement total amount");
            Check.That(bankTransactionFileStatementTotalAmountMismatch.Examples[0].ShortMessage).IsEqualTo("Statement total amount mismatch.");

            // ------------------------------------------------------------------
            // TEMPERATURE_BELOW_ABSOLUTE_ZERO
            // ------------------------------------------------------------------

            Check.That(temperatureBelowAbsoluteZero.Code).IsEqualTo("TEMPERATURE_BELOW_ABSOLUTE_ZERO");
            Check.That(temperatureBelowAbsoluteZero.Title).IsEqualTo("Temperature below absolute zero");
            Check.That(temperatureBelowAbsoluteZero.Explanation).IsEqualTo("This error occurs when trying to instantiate a temperature with a value that is below absolute zero.");
            Check.That(temperatureBelowAbsoluteZero.BusinessRule).IsEqualTo("Temperature cannot go below absolute zero because absolute zero is the point where particles have minimum possible energy.");

            Check.That(temperatureBelowAbsoluteZero.Exception).IsEqualTo(typeof(InvalidTemperatureException));
            Check.That(temperatureBelowAbsoluteZero.ErrorSource).IsEqualTo(typeof(Temperature));
            Check.That(temperatureBelowAbsoluteZero.FactoryMethodName).IsEqualTo("BelowAbsoluteZero");

            // Diagnostics

            Check.That(temperatureBelowAbsoluteZero.Diagnostics).CountIs(5);

            // 1
            Check.That(temperatureBelowAbsoluteZero.Diagnostics[0].Cause).IsEqualTo("The value entered manually by a user is invalid.");
            Check.That(temperatureBelowAbsoluteZero.Diagnostics[0].Type).IsEqualTo(ErrorCauseType.Input);
            Check.That(temperatureBelowAbsoluteZero.Diagnostics[0].AnalysisLead).IsEqualTo("Verify the value entered by the user and assess its compliance with domain rules.");

            // 2
            Check.That(temperatureBelowAbsoluteZero.Diagnostics[1].Cause).IsEqualTo("The value received from an external system (API, message, etc.) is invalid.");
            Check.That(temperatureBelowAbsoluteZero.Diagnostics[1].Type).IsEqualTo(ErrorCauseType.Input);
            Check.That(temperatureBelowAbsoluteZero.Diagnostics[1].AnalysisLead).IsEqualTo("Check the data provided by the upstream system and evaluate its validity against domain rules.");

            // 3
            Check.That(temperatureBelowAbsoluteZero.Diagnostics[2].Cause).IsEqualTo("The value was loaded from corrupted or outdated persisted data.");
            Check.That(temperatureBelowAbsoluteZero.Diagnostics[2].Type).IsEqualTo(ErrorCauseType.Input);
            Check.That(temperatureBelowAbsoluteZero.Diagnostics[2].AnalysisLead).IsEqualTo("Examine the persisted data source to determine whether stored values comply with current domain rules.");

            // 4
            Check.That(temperatureBelowAbsoluteZero.Diagnostics[3].Cause).IsEqualTo("The value was computed internally without using domain-safe methods.");
            Check.That(temperatureBelowAbsoluteZero.Diagnostics[3].Type).IsEqualTo(ErrorCauseType.System);
            Check.That(temperatureBelowAbsoluteZero.Diagnostics[3].AnalysisLead).IsEqualTo("Inspect the internal computation logic to confirm that domain invariants are preserved.");

            // 5
            Check.That(temperatureBelowAbsoluteZero.Diagnostics[4].Cause).IsEqualTo("The value originates from system configuration or defaults that are incorrect or outdated.");
            Check.That(temperatureBelowAbsoluteZero.Diagnostics[4].Type).IsEqualTo(ErrorCauseType.Input);
            Check.That(temperatureBelowAbsoluteZero.Diagnostics[4].AnalysisLead).IsEqualTo("Review the relevant configuration or default parameters to assess their compliance with domain rules.");

            // Examples

            Check.That(temperatureBelowAbsoluteZero.Examples).CountIs(2);

            Check.That(temperatureBelowAbsoluteZero.Examples[0].DetailedMessage).Contains("Failed to instantiate temperature");
            Check.That(temperatureBelowAbsoluteZero.Examples[0].ShortMessage).IsEqualTo("Temperature is below absolute zero.");
            Check.That(temperatureBelowAbsoluteZero.Examples[1].DetailedMessage).Contains("Failed to instantiate temperature");
            Check.That(temperatureBelowAbsoluteZero.Examples[1].ShortMessage).IsEqualTo("Temperature is below absolute zero.");
        }

    }

}