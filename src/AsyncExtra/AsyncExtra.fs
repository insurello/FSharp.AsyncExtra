namespace global

[<RequireQualifiedAccessAttribute>]
module Async =
    let singleton: 'value -> Async<'value> = async.Return

    let bind: ('x -> Async<'y>) -> Async<'x> -> Async<'y> = fun f x -> async.Bind(x, f)

    let map: ('x -> 'y) -> Async<'x> -> Async<'y> = fun f x -> bind (f >> singleton) x

type AsyncResult<'x, 'err> = Async<Result<'x, 'err>>

[<RequireQualifiedAccessAttribute>]
module AsyncResult =
    let fromResult: Result<'x, 'err> -> AsyncResult<'x, 'err> = Async.singleton

    let map: ('x -> 'y) -> AsyncResult<'x, 'err> -> AsyncResult<'y, 'err> =
        fun f asyncResultX -> Async.map (Result.map f) asyncResultX

    let mapError: ('errX -> 'errY) -> AsyncResult<'x, 'errX> -> AsyncResult<'x, 'errY> =
        fun f asyncResultX -> Async.map (Result.mapError f) asyncResultX

    let bind: ('x -> AsyncResult<'y, 'err>) -> AsyncResult<'x, 'err> -> AsyncResult<'y, 'err> =
        fun f asyncResultX ->
            asyncResultX
            |> Async.bind (function
                | Ok x -> f x

                | Error err -> Async.singleton (Error err))

    let sequence: AsyncResult<'x, 'error> list -> AsyncResult<'x list, 'error> =
        let folder: AsyncResult<'x list, 'error> -> AsyncResult<'x, 'error> -> AsyncResult<'x list, 'error> =
            fun acc nextAsyncResult ->
                acc |> bind (fun okValues -> nextAsyncResult |> map (fun nextOkValue -> nextOkValue :: okValues))

        fun asyncs ->
            asyncs
            |> List.fold folder (fromResult (Ok []))
            |> map List.rev