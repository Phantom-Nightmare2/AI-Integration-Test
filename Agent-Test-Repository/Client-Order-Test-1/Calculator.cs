using System;

namespace ClientOrderTest1
{
    internal class Calculator
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

- ★HERCULES//コード合格
- using System;

namespace ClientOrderTest1
{
    internal class Calculator
    {
        static void Main(string[] args)
        {
            // The client wants this program to add two numbers and display the result.
            int firstNumber = 10;
            // Fixed: added the missing semicolon at the end of this statement (compile error CS1002).
            int secondNumber = 5;

            // Intentional error: the variable name below does not match the declared variable.
            // Fixed: changed "secondNum" to "secondNumber" so it refers to the variable declared above (compile error CS0103).
            int total = firstNumber + secondNumber;

            // Intentional error: this method name is misspelled.
            // Fixed: corrected "WritLine" to "WriteLine", the actual Console method name (compile error CS0117).
            Console.WriteLine("Total: " + total);

            // Intentional logic error: this condition should report whether the total is positive.
            // Fixed: changed "total < 0" to "total > 0". A positive number is greater than zero;
            // the old condition was true only for negative totals, so the message never appeared for 15.
            if (total > 0)
            {
                Console.WriteLine("The total is positive.");
            }
        }
    }
}
// テスト ヘラクレスがしました
