using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CardOverflow.Debug {
  public class MyObjectDumper {
    private int _currentIndent;
    private readonly int _indentSize;
    private readonly StringBuilder _stringBuilder;
    private readonly Dictionary<object, int> _hashListOfFoundElements;
    private readonly char _indentChar;
    private readonly int _depth;
    private int _currentLine;

    private MyObjectDumper(int depth, int indentSize, char indentChar) {
      _depth = depth;
      _indentSize = indentSize;
      _indentChar = indentChar;
      _stringBuilder = new StringBuilder();
      _hashListOfFoundElements = new Dictionary<object, int>();
    }

    public static string Dump(object element, int depth = 4, int indentSize = 2, char indentChar = ' ') {
      var instance = new MyObjectDumper(depth, indentSize, indentChar);
      return instance.DumpElement(element, true);
    }

    private string DumpElement(object element, bool isTopOfTree = false) {
      if (_currentIndent > _depth) { return null; }
      if (element == null || element is string) {
        Write(FormatValue(element));
      } else if (element is ValueType) {
        Type objectType = element.GetType();
        bool isWritten = false;
        if (objectType.IsGenericType) {
          Type baseType = objectType.GetGenericTypeDefinition();
          if (baseType == typeof(KeyValuePair<,>)) {
            isWritten = true;
            Write("Key:");
            _currentIndent++;
            DumpElement(objectType.GetProperty("Key").GetValue(element, null));
            _currentIndent--;
            Write("Value:");
            _currentIndent++;
            DumpElement(objectType.GetProperty("Value").GetValue(element, null));
            _currentIndent--;
          }
        }
        if (!isWritten) {
          Write(FormatValue(element));
        }
      } else {
        var enumerableElement = element as IEnumerable;
        if (enumerableElement != null) {
          foreach (object item in enumerableElement) {
            if (item is IEnumerable && !(item is string)) {
              _currentIndent++;
              DumpElement(item);
              _currentIndent--;
            } else {
              DumpElement(item);
            }
          }
        } else {
          Type objectType = element.GetType();
          Write($"{{{objectType.FullName}(HashCode:{element.GetHashCode()})}}");
          if (!AlreadyDumped(element)) {
            _currentIndent++;
            MemberInfo[] members = objectType.GetMembers(BindingFlags.Public | BindingFlags.Instance);
            foreach (var memberInfo in members) {
              var fieldInfo = memberInfo as FieldInfo;
              var propertyInfo = memberInfo as PropertyInfo;

              if (fieldInfo == null && (propertyInfo == null || !propertyInfo.CanRead || propertyInfo.GetIndexParameters().Length > 0))
                continue;

              var type = fieldInfo != null ? fieldInfo.FieldType : propertyInfo.PropertyType;
              object value;
              try {
                value = fieldInfo != null
                  ? fieldInfo.GetValue(element)
                  : propertyInfo.GetValue(element, null);
              } catch (Exception e) {
                Write($"{memberInfo.Name} failed with:{e.GetBaseException().Message}");
                continue;
              }

              if (type.IsValueType || type == typeof(string) || value == null) {
                Write($"{memberInfo.Name}: {FormatValue(value)}");
              } else {
                var isEnumerable = typeof(IEnumerable).IsAssignableFrom(type);
                if (isEnumerable) {
                  Write($"{memberInfo.Name}: {(((IEnumerable) value).Cast<object>().Any() ? "..." : "[]")}");
                } else {
                  Write(memberInfo.Name + ": { }");
                }

                _currentIndent++;
                DumpElement(value);
                _currentIndent--;
              }
            }
            _currentIndent--;
          }
        }
      }

      return isTopOfTree ? _stringBuilder.ToString() : null;
    }

    private bool AlreadyDumped(object value) {
      if (value == null)
        return false;
      if (_hashListOfFoundElements.TryGetValue(value, out int lineNo)) {
        Write($"(reference already dumped - line:{lineNo})");
        return true;
      }
      _hashListOfFoundElements.Add(value, _currentLine);
      return false;
    }

    private void Write(string value) {
      var space = new string(_indentChar, _currentIndent * _indentSize);
      _stringBuilder.AppendLine(space + value);
      _currentLine++;
    }

    private string FormatValue(object o) {
      if (o == null)
        return ("null");

      if (o is string)
        return "\"" + (string) o + "\"";

      if (o is char) {
        if (o.Equals('\0')) {
          return "''";
        } else {
          return "'" + (char) o + "'";
        }
      }

      if (o is ValueType)
        return (o.ToString());

      if (o is IEnumerable)
        return ("...");

      return ("{ }");
    }
  }
}