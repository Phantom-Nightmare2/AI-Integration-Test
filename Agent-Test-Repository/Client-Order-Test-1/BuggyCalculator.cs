using System;

namespace ClientOrderTest1
{
    internal class BuggyCalculator
    {
        static void Main(string[] args)
        {
            // The client wants this program to add two numbers and display the result.
            int firstNumber = 10;
            int secondNumber = 5

            // Intentional error: the variable name below does not match the declared variable.
            int total = firstNumber + secondNum;

            // Intentional error: this method name is misspelled.
            Console.WritLine("Total: " + total);

            // Intentional logic error: this condition should report whether the total is positive.
            if (total < 0)
            {
                Console.WriteLine("The total is positive.");
            }
        }
    }
}
