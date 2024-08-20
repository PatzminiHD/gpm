using gpm.Model;
using PatzminiHD.CSLib.Input.Console;
using PatzminiHD.CSLib.Output.Console.Table;

namespace gpm.View
{
    public class MainView
    {
        private MainModel model;
        private (List<(Entry, uint)>, uint) tableHeaders =
        (
            new()
            {
                (new("Name"), 25), (new("Update Available"), 17),
            }, 1
        );

        private Base table;
        private bool selectedButton = true;

        public MainView()
        {
            model = new();
            table = new(model.TableValues, tableHeaders, (uint)Console.WindowHeight - 7);
            table.AutoDraw = true;
            table.TopPos = 2;
            table.LeftPos = 2;
        }
        public void Start()
        {
            Console.Title = model.Title;
            ConsoleKey key = ConsoleKey.None;
            table.Draw();
            while(key != ConsoleKey.Escape)
            {
                Render();
                key = Console.ReadKey(true).Key;

                switch(key)
                {
                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.RightArrow:
                        selectedButton = !selectedButton;
                        break;

                    case ConsoleKey.UpArrow:
                        if(table.FirstShownRow > 0)
                            table.FirstShownRow -= 1;
                        break;

                    case ConsoleKey.DownArrow:
                        if (table.FirstShownRow < table.TableValues.Count - 1)
                            table.FirstShownRow += 1;
                        break;

                    case ConsoleKey.Enter:
                        if(selectedButton)
                            model.RefreshUpdate();
                        else
                        {
                            model.UpdateAll();
                            model.RefreshUpdate();
                        }
                        break;
                }
            }
        }

        private void Render()
        {
            Console.Clear();
            while(Console.WindowHeight < 30 || Console.WindowWidth < 50)
            {
                Console.Clear();
                Console.SetCursorPosition(0, 0);
                Console.WriteLine("Minimum console size: 50x30\n" +
                    $"Current console size: {Console.WindowWidth}x{Console.WindowHeight}");

            }

            string title = model.Title;
            Console.SetCursorPosition((Console.WindowHeight / 2) - (title.Length / 2), 0);
            Console.WriteLine(title);

            //if(table.TableValues != model.TableValues)
                table.TableValues = model.TableValues;

            Console.SetCursorPosition(2, Console.WindowHeight - 2);
            if(selectedButton)
            {
                PatzminiHD.CSLib.Output.Console.Color.SwapForegroundBackgroundColor();
                Console.Write("Refresh");
                PatzminiHD.CSLib.Output.Console.Color.SwapForegroundBackgroundColor();
                Console.Write(" Update all");
            }
            else
            {
                Console.Write("Refresh ");
                PatzminiHD.CSLib.Output.Console.Color.SwapForegroundBackgroundColor();
                Console.Write("Update all");
                PatzminiHD.CSLib.Output.Console.Color.SwapForegroundBackgroundColor();
            }
        }
    }
}
