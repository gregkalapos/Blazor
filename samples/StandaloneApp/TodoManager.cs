using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StandaloneApp
{
	public class Todo
	{
		public String Text { get; set; } = String.Empty;
		public Boolean IsDone { get; set; }
	}
	public static class TodoManager
	{
		public static List<Todo> GetAllTodos() => new List<Todo>
													{
														new Todo{Text = "TodoItem1"},
														new Todo{Text = "TodoItem2"}
													};

	}
}
