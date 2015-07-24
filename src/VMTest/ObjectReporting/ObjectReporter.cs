using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using TestConsole.OutputFormatting;
using TestConsole.OutputFormatting.Internal;
using TestConsole.OutputFormatting.ReportDefinitions;
using TestConsoleLib;
using VMTest.Utilities;

namespace VMTest.ObjectReporting
{
    /// <summary>
    /// Report the state of an object. If the object implements <see cref="INotifyPropertyChanged"/>
    /// report all child objects. If the object does not implement <see cref="INotifyPropertyChanged"/>,
    /// only report on its properties.
    /// </summary>
    internal class ObjectReporter<T>
    {
        private readonly ReportType _reportType;

        class PropertyReportType
        {
            public PropertyInfo PropertyInfo { get; set; }
            public ColumnConfig ColumnConfig { get; set; }
            public string Name { get; set; }
            public object Value { get; set; }
        }

        public ObjectReporter(ReportType reportType = ReportType.Table)
        {
            _reportType = reportType;
        }

        public void Report(T item, Output output, ReportType reportType = ReportType.Default)
        {
            if (reportType == ReportType.Default)
                reportType = _reportType;

            switch (reportType)
            {
                case ReportType.PropertyList:
                    ListProperties(item, output);
                    break;

                case ReportType.Table:
                    ShowAsTable(item, output);
                    break;

                case ReportType.NoReport:
                    break;
            }
        }

        private static void ShowAsTable(T item, Output output)
        {
            var report = MakeTableReport(item);
            output.FormatTable(report);
        }

        private static Report<T> MakeTableReport(T item)
        {
            var items = (item as object == null) 
            ? new T[] {}
            : new[] {item};
            return items.AsReport(rep => DesignReport(rep));
        }

        internal static void DesignReport(ReportParameters<T> rep, string title = null)
        {
            if (title != null)
                rep.Title(title);

            if (Type.GetTypeCode(typeof (T)) != TypeCode.Object)
            {
                rep.AddColumn(i => i, cc => cc.Heading(typeof (T).Name));
                return;
            }

            var columns = 0;
            var childReports = new List<PropertyInfo>();
            foreach (var propertyInfo in typeof(T).GetProperties())
            {
                if (propertyInfo.GetIndexParameters().Any())
                    continue;

                if (Type.GetTypeCode(propertyInfo.PropertyType) == TypeCode.Object)
                    childReports.Add(propertyInfo);
                else
                {
                    AddPropertyToReport(rep, propertyInfo);
                    ++columns;
                }
            }

            if (columns == 0)
            {
                rep.AddColumn(t => (string)null, cc => cc.Heading(typeof(T).Name));
            }

            foreach (var propertyInfo in childReports)
            {
                AddChildToReport(rep, propertyInfo);
            }

            if (typeof (IEnumerable).IsAssignableFrom(typeof (T)))
            {
                AddCollectionItemsChildReport(rep);
            }
        }

        private static void AddCollectionItemsChildReport(ReportParameters<T> rep)
        {
            var method = typeof (ObjectReporter<T>)
                .GetMethod("AddChildReport",
                    BindingFlags.NonPublic | BindingFlags.Static);
            Debug.Assert(method != null);
            var childType = DetermineChildType(typeof (T));
            var typedMethod = method.MakeGenericMethod(typeof (T), childType);
            var getterFn = MakeReturnSelfGetterFn().Compile();
            MethodInvoker.Invoke(typedMethod, null, getterFn, rep, string.Empty);
        }

        private static Expression<Func<T, T>> MakeReturnSelfGetterFn()
        {
            var parameter = Expression.Parameter(typeof (T));
            return Expression.Lambda<Func<T, T>>(parameter, parameter);
        }

        private static Type DetermineChildType(Type type)
        {
            Type enumeratedType;
            if (!GetEnumeratedType(type, out enumeratedType))
                return typeof (Object);

            return enumeratedType;
        }

        private static void AddPropertyToReport(ReportParameters<T> rep, PropertyInfo propertyInfo)
        {
            var method = typeof (ObjectReporter<T>)
                .GetMethod("AddPropertyWithKnownTypeToReport", 
                BindingFlags.NonPublic | BindingFlags.Static);
            Debug.Assert(method != null);
            var typedMethod = method.MakeGenericMethod(typeof (T), propertyInfo.PropertyType);
            MethodInvoker.Invoke(typedMethod, null, rep, propertyInfo);
        }

        // ReSharper disable once UnusedMember.Global
        internal static void AddPropertyWithKnownTypeToReport<TItem, TProp>(ReportParameters<TItem> rep,
            PropertyInfo property)
        {
            var getterFn = MakeGetterFn<TItem, TProp>(property);
            rep.AddColumn(getterFn, cc => cc.Heading(property.Name));
        }

        private static void AddChildToReport(ReportParameters<T> rep, PropertyInfo propertyInfo)
        {
            var method = typeof(ObjectReporter<T>)
                .GetMethod("AddChildWithKnownTypeToReport",
                BindingFlags.NonPublic | BindingFlags.Static);
            Debug.Assert(method != null);
            var typedMethod = method.MakeGenericMethod(typeof(T), propertyInfo.PropertyType);
            MethodInvoker.Invoke(typedMethod, null, rep, propertyInfo);
        }

        // ReSharper disable once UnusedMember.Global
        internal static void AddChildWithKnownTypeToReport<TItem, TProp>(ReportParameters<TItem> rep,
            PropertyInfo property)
        {
            var method = typeof(ObjectReporter<TItem>)
                .GetMethod("AddChildReport",
                BindingFlags.NonPublic | BindingFlags.Static);
            Debug.Assert(method != null);

            Type enumeratedType;
            if (GetEnumeratedType(typeof (TProp), out enumeratedType))
            {
                var getterFn = MakeGetterFn<TItem, TProp>(property)
                    .Compile();
                var typedMethod = method.MakeGenericMethod(typeof(T), enumeratedType);
                MethodInvoker.Invoke(typedMethod, null, getterFn, rep, property.Name);
            }
            else
            {
                var getterFn = MakeEnumerableGetterFn<TItem, TProp>(property)
                    .Compile();
               var typedMethod = method.MakeGenericMethod(typeof(T), typeof(TProp));
               MethodInvoker.Invoke(typedMethod, null, getterFn, rep, property.Name);
            }

        }

        // ReSharper disable once UnusedMember.Global
        private static void AddChildReport<TContainer, TChild>(Func<TContainer,IEnumerable<TChild>>  getterFn, ReportParameters<TContainer> rep, string title)
        {
            var method = typeof(ObjectReporter<TChild>)
                .GetMethod("DesignReport",
                BindingFlags.NonPublic | BindingFlags.Static);
            Debug.Assert(method != null);

            var repParam = Expression.Parameter(typeof(ReportParameters<TChild>));
            var titleParam = Expression.Constant(title);
            var reporter = Expression.Lambda<Action<ReportParameters<TChild>>>(
                Expression.Call(method, repParam, titleParam), repParam)
                .Compile();
            rep.AddChild(getterFn, reporter);
        }

        private static bool GetEnumeratedType(Type type, out Type enumeratedType)
        {
            var enumerable = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IEnumerable<>));
            if (enumerable != null)
            {
                enumeratedType = enumerable.GetGenericArguments()[0];
                return true;
            }

            enumeratedType = null;
            return false;
        }

        private static Expression<Func<TItem, TProp>>  MakeGetterFn<TItem, TProp>(PropertyInfo property)
        {
            var item = Expression.Parameter(typeof (TItem));
            var getter = Expression.MakeMemberAccess(item, property);
            var getterFn = Expression.Lambda<Func<TItem, TProp>>(
                getter, new[] {item});
            return getterFn;
        }

        private static Expression<Func<TItem, IEnumerable<TProp>>>  MakeEnumerableGetterFn<TItem, TProp>(PropertyInfo property)
        {
            var item = Expression.Parameter(typeof (TItem));
            var getter = Expression.MakeMemberAccess(item, property);
            var makeArray = Expression.NewArrayInit(typeof (TProp), getter);
            var getterFn = Expression.Lambda<Func<TItem, IEnumerable<TProp>>>(
                makeArray, new[] {item});
            return getterFn;
        }

        private void ListProperties(T item, Output output)
        {
            var props = GetPropertyColumns(item);
            output.FormatTable(props
                .Select(p => new
                {
                    p.Name, p.Value
                }),
                ReportFormattingOptions.OmitHeadings);
        }

        private static IEnumerable<PropertyReportType> GetPropertyColumns(T item)
        {
            var itemType = typeof (T);
            var props = itemType.GetProperties().Select(p => MakePropertyReportType(p, item));
            return props;
        }

        private static PropertyReportType MakePropertyReportType(PropertyInfo prop, object item)
        {
            var value = prop.GetValue(item, null) ?? "NULL";
            if (!(value is string) && value is IEnumerable)
            {
                value = RenderTable(value) ?? value;
            }
            else if (!(value is string) && Type.GetTypeCode(value.GetType()) == TypeCode.Object)
            {
                var enumerableValue = SingleEnumerator.EnumerateSingle(value);
                value = RenderTable(enumerableValue) ?? value;
            }

            return new PropertyReportType
            {
                PropertyInfo = prop,
                Name = prop.Name,
                Value = value
            };
        }

        private static object RenderTable(object value)
        {
            var valueType = value.GetType();
            var baseEnumerable = valueType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IEnumerable<>))
                .FirstOrDefault();
            if (baseEnumerable != null)
            {
                var renderable = new RecordingConsoleAdapter();
                var formatMethod = renderable.GetType()
                    .GetMethods()
                    .FirstOrDefault(m => m.Name == "FormatTable"
                                         && m.GetParameters().Count() == 3
                                         && m.IsGenericMethodDefinition
                                         && m.GetParameters()[1].ParameterType == typeof (ReportFormattingOptions));
                if (formatMethod != null)
                {

                    formatMethod = formatMethod.MakeGenericMethod(valueType.GetGenericArguments()[0]);
                    formatMethod.Invoke(renderable, new[]
                    {
                        value,
                        ReportFormattingOptions.Default,
                        null
                    });
                    return renderable;
                }
            }
            return null;
        }

    }

    internal class SingleEnumerator
    {
        internal static IEnumerable<T> ReturnEnumerable<T>(T item)
        {
            yield return item;
        }

        internal static object EnumerateSingle(object value)
        {
            var method = typeof(SingleEnumerator)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .Single(m => m.Name == "ReturnEnumerable");
            var genMethod = method.MakeGenericMethod(value.GetType());
            return genMethod.Invoke(null, new[] { value });
        }
    }
}
