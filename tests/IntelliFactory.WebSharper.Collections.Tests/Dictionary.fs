// $begin{copyright}
//
// This file is part of WebSharper
//
// Copyright (c) 2008-2014 IntelliFactory
//
// Licensed under the Apache License, Version 2.0 (the "License"); you
// may not use this file except in compliance with the License.  You may
// obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
// implied.  See the License for the specific language governing
// permissions and limitations under the License.
//
// $end{copyright}

module IntelliFactory.WebSharper.Collections.Tests.Dictionary

open System
open System.Collections.Generic
open IntelliFactory.WebSharper
open IntelliFactory.WebSharper.Testing

type Foo = {Foo:string}

[<JavaScript>]
let Tests =
    Section "Dictionary"

    Test "New" {
        let x = Dictionary()
        let y = Dictionary(10)
        Assert.True(true)
    }

    Test "Add" {
        let d = Dictionary()
        d.Add(1,"a")
        d.[1] =? "a"
    }

    Test "Clear" {
        let d = Dictionary()
        d.Add(1,"a")
        d.Clear()
        d.Count =? 0
        Assert.Raises (fun () -> ignore (d.[1]))
    }

    Test "Count" {
        let d = Dictionary()
        d.Count =? 0
        [1..100]
        |> List.iter (fun i -> d.Add(i,i))
        d.Count =? 100
    }

    Test "Objects" {
        let d = Dictionary()
        let foo = ""
        let f1 = {Foo = "1"}
        let f2 = {Foo = "2"}
        let f3 = f1
        d.Add(f1,1)
        d.Add(f2,2)
        d.Item f3 =? 1
        d.Item f2 =? 2
    }

    Test "ContainsKey" {
        let d = Dictionary ()
        let foo = ""
        let f1 = {Foo = "1"}
        let f2 = {Foo = "2"}
        let f3 = f1
        d.Add(f1,1)
        d.ContainsKey f1 =? true
        d.ContainsKey f2 =? false
    }

    Test "GetEnumerator" {
        let d = Dictionary()
        let foo = ""
        let f1 = {Foo = "1"}
        let f2 = {Foo = "2"}
        let f3 = f1
        d.Add(f1,1)
        d.Add(f2,2)
        let enum = (d :> seq<_>).GetEnumerator()
        let kArr = Array.zeroCreate d.Count
        let vArr = Array.zeroCreate d.Count
        let mutable ix = 0
        let _ =
            while enum.MoveNext() do
                let kvp = enum.Current
                vArr.[ix] <- kvp.Value
                kArr.[ix] <- kvp.Key
                ix <- ix + 1
        vArr =? [| 1; 2 |]
        Array.map (fun o -> o.Foo) kArr =? [| "1"; "2" |]
    }
