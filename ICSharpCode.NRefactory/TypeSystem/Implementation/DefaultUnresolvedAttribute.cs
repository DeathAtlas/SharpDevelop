﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation of <see cref="IUnresolvedAttribute"/>.
	/// </summary>
	public class DefaultUnresolvedAttribute : AbstractFreezable, IUnresolvedAttribute, IFreezable
	{
		readonly ITypeReference attributeType;
		DomRegion region;
		IList<ITypeReference> constructorParameterTypes;
		IList<IConstantValue> positionalArguments;
		IList<KeyValuePair<IMemberReference, IConstantValue>> namedArguments;
		
		public DefaultUnresolvedAttribute(ITypeReference attributeType)
		{
			if (attributeType == null)
				throw new ArgumentNullException("attributeType");
			this.attributeType = attributeType;
		}
		
		public DefaultUnresolvedAttribute(ITypeReference attributeType, IEnumerable<ITypeReference> constructorParameterTypes)
		{
			if (attributeType == null)
				throw new ArgumentNullException("attributeType");
			this.attributeType = attributeType;
			this.ConstructorParameterTypes.AddRange(constructorParameterTypes);
		}
		
		protected override void FreezeInternal()
		{
			base.FreezeInternal();
			constructorParameterTypes = FreezableHelper.FreezeList(constructorParameterTypes);
			namedArguments = FreezableHelper.FreezeList(namedArguments);
			foreach (var pair in namedArguments) {
				FreezableHelper.Freeze(pair.Key);
				FreezableHelper.Freeze(pair.Value);
			}
		}
		
		public ITypeReference AttributeType {
			get { return attributeType; }
		}
		
		public DomRegion Region {
			get { return region; }
			set {
				FreezableHelper.ThrowIfFrozen(this);
				region = value;
			}
		}
		
		public IList<ITypeReference> ConstructorParameterTypes {
			get {
				if (constructorParameterTypes == null)
					constructorParameterTypes = new List<ITypeReference>();
				return constructorParameterTypes;
			}
		}
		
		public IList<IConstantValue> PositionalArguments {
			get {
				if (positionalArguments == null)
					positionalArguments = new List<IConstantValue>();
				return positionalArguments;
			}
		}
		
		public IList<KeyValuePair<IMemberReference, IConstantValue>> NamedArguments {
			get {
				if (namedArguments == null)
					namedArguments = new List<KeyValuePair<IMemberReference, IConstantValue>>();
				return namedArguments;
			}
		}
		
		public void AddNamedFieldArgument(string fieldName, IConstantValue value)
		{
			throw new NotImplementedException();
		}
		
		public void AddNamedFieldArgument(string fieldName, ITypeReference valueType, object value)
		{
			AddNamedFieldArgument(fieldName, new SimpleConstantValue(valueType, value));
		}
		
		public void AddNamedPropertyArgument(string propertyName, IConstantValue value)
		{
			throw new NotImplementedException();
		}
		
		public void AddNamedPropertyArgument(string propertyName, ITypeReference valueType, object value)
		{
			AddNamedPropertyArgument(propertyName, new SimpleConstantValue(valueType, value));
		}
		
		public IAttribute CreateResolvedAttribute(ITypeResolveContext context)
		{
			throw new NotImplementedException();
		}
		
		sealed class DefaultResolvedAttribute : IAttribute, IResolved
		{
			readonly DefaultUnresolvedAttribute unresolved;
			readonly ITypeResolveContext context;
			readonly IType attributeType;
			readonly IList<ResolveResult> positionalArguments;
			
			// cannot use ProjectedList because KeyValuePair is value type
			IList<KeyValuePair<IMember, ResolveResult>> namedArguments;
			
			IMethod constructor;
			volatile bool constructorResolved;
			
			public DefaultResolvedAttribute(DefaultUnresolvedAttribute unresolved, ITypeResolveContext context)
			{
				this.unresolved = unresolved;
				this.context = context;
				
				this.attributeType = unresolved.AttributeType.Resolve(context);
				this.positionalArguments = unresolved.PositionalArguments.Resolve(context);
			}
			
			public IType AttributeType {
				get { return attributeType; }
			}
			
			public DomRegion Region {
				get { return unresolved.Region; }
			}
			
			public IMethod Constructor {
				get {
					if (!constructorResolved) {
						constructor = ResolveConstructor();
						constructorResolved = true;
					}
					return constructor;
				}
			}
			
			IMethod ResolveConstructor()
			{
				var parameterTypes = unresolved.ConstructorParameterTypes.Resolve(context);
				foreach (var ctor in attributeType.GetConstructors(m => m.Parameters.Count == parameterTypes.Count)) {
					bool ok = true;
					for (int i = 0; i < parameterTypes.Count; i++) {
						if (!ctor.Parameters[i].Type.Equals(parameterTypes[i])) {
							ok = false;
							break;
						}
					}
					if (ok)
						return ctor;
				}
				return null;
			}
			
			public IList<ResolveResult> PositionalArguments {
				get { return positionalArguments; }
			}
			
			public IList<KeyValuePair<IMember, ResolveResult>> NamedArguments {
				get {
					var namedArgs = this.namedArguments;
					if (namedArgs != null) {
						LazyInit.ReadBarrier();
						return namedArgs;
					} else {
						namedArgs = new List<KeyValuePair<IMember, ResolveResult>>();
						foreach (var pair in unresolved.NamedArguments) {
							IMember member = pair.Key.Resolve(context);
							if (member != null) {
								ResolveResult val = pair.Value.Resolve(context);
								namedArgs.Add(new KeyValuePair<IMember, ResolveResult>(member, val));
							}
						}
						return LazyInit.GetOrSet(ref this.namedArguments, namedArgs);
					}
				}
			}
			
			public ICompilation Compilation {
				get { return context.Compilation; }
			}
		}
	}
}
