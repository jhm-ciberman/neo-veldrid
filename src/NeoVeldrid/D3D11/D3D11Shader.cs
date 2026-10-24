using System;
using System.Runtime.InteropServices;
using System.Text;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;

namespace NeoVeldrid.D3D11;

internal unsafe partial class D3D11Shader : Shader
{
    private string _name;

    private ComPtr<ID3D11DeviceChild> _deviceShader;
    private bool _disposed;
    public ComPtr<ID3D11DeviceChild> DeviceShader => _deviceShader;
    public byte[] Bytecode { get; internal set; }

    public D3D11Shader(ID3D11Device* device, ShaderDescription description)
        : base(description.Stage, description.EntryPoint)
    {
        if (description.ShaderBytes.Length > 4
            && description.ShaderBytes[0] == 0x44
            && description.ShaderBytes[1] == 0x58
            && description.ShaderBytes[2] == 0x42
            && description.ShaderBytes[3] == 0x43)
        {
            Bytecode = Util.ShallowClone(description.ShaderBytes);
        }
        else
        {
            Bytecode = CompileCode(description);
        }

        fixed (byte* pBytecode = Bytecode)
        {
            switch (description.Stage)
            {
                case ShaderStages.Vertex:
                {
                    ID3D11VertexShader* pShader;
                    SilkMarshal.ThrowHResult(device->CreateVertexShader(pBytecode, (nuint)Bytecode.Length, null, &pShader));
                    _deviceShader = default;
                    _deviceShader.Handle = (ID3D11DeviceChild*)pShader;
                    break;
                }
                case ShaderStages.Geometry:
                {
                    ID3D11GeometryShader* pShader;
                    SilkMarshal.ThrowHResult(device->CreateGeometryShader(pBytecode, (nuint)Bytecode.Length, null, &pShader));
                    _deviceShader = default;
                    _deviceShader.Handle = (ID3D11DeviceChild*)pShader;
                    break;
                }
                case ShaderStages.TessellationControl:
                {
                    ID3D11HullShader* pShader;
                    SilkMarshal.ThrowHResult(device->CreateHullShader(pBytecode, (nuint)Bytecode.Length, null, &pShader));
                    _deviceShader = default;
                    _deviceShader.Handle = (ID3D11DeviceChild*)pShader;
                    break;
                }
                case ShaderStages.TessellationEvaluation:
                {
                    ID3D11DomainShader* pShader;
                    SilkMarshal.ThrowHResult(device->CreateDomainShader(pBytecode, (nuint)Bytecode.Length, null, &pShader));
                    _deviceShader = default;
                    _deviceShader.Handle = (ID3D11DeviceChild*)pShader;
                    break;
                }
                case ShaderStages.Fragment:
                {
                    ID3D11PixelShader* pShader;
                    SilkMarshal.ThrowHResult(device->CreatePixelShader(pBytecode, (nuint)Bytecode.Length, null, &pShader));
                    _deviceShader = default;
                    _deviceShader.Handle = (ID3D11DeviceChild*)pShader;
                    break;
                }
                case ShaderStages.Compute:
                {
                    ID3D11ComputeShader* pShader;
                    SilkMarshal.ThrowHResult(device->CreateComputeShader(pBytecode, (nuint)Bytecode.Length, null, &pShader));
                    _deviceShader = default;
                    _deviceShader.Handle = (ID3D11DeviceChild*)pShader;
                    break;
                }
                default:
                    throw Illegal.Value<ShaderStages>();
            }
        }
    }

    private static byte[] CompileCode(ShaderDescription description)
    {
        string profile;
        switch (description.Stage)
        {
            case ShaderStages.Vertex:
                profile = "vs_5_0";
                break;
            case ShaderStages.Geometry:
                profile = "gs_5_0";
                break;
            case ShaderStages.TessellationControl:
                profile = "hs_5_0";
                break;
            case ShaderStages.TessellationEvaluation:
                profile = "ds_5_0";
                break;
            case ShaderStages.Fragment:
                profile = "ps_5_0";
                break;
            case ShaderStages.Compute:
                profile = "cs_5_0";
                break;
            default:
                throw Illegal.Value<ShaderStages>();
        }

        // D3DCOMPILE_DEBUG = 0x1, D3DCOMPILE_OPTIMIZATION_LEVEL3 = 0x8000
        uint flags = description.Debug ? 0x1u : 0x8000u;

        ID3D10Blob* pResult = null;
        ID3D10Blob* pError = null;

        fixed (byte* pSource = description.ShaderBytes)
        {
            D3DCompile(
                pSource, (nuint)description.ShaderBytes.Length,
                null,
                null,
                null,
                description.EntryPoint,
                profile,
                flags, 0,
                &pResult, &pError);

            if (pResult == null)
            {
                string errorMsg = string.Empty;
                if (pError != null)
                {
                    byte* errorPtr = (byte*)pError->GetBufferPointer();
                    nuint errorSize = pError->GetBufferSize();
                    errorMsg = Encoding.ASCII.GetString(errorPtr, (int)errorSize);
                    pError->Release();
                }
                throw new NeoVeldridException($"Failed to compile HLSL code: {errorMsg}");
            }

            byte* resultPtr = (byte*)pResult->GetBufferPointer();
            nuint resultSize = pResult->GetBufferSize();
            byte[] bytecode = new byte[resultSize];
            new Span<byte>(resultPtr, (int)resultSize).CopyTo(bytecode);

            if (pError != null) pError->Release();
            pResult->Release();

            return bytecode;
        }
    }

    // Imported directly: Silk.NET's D3DCompiler class reaches its default native library resolver from its static
    // constructor, which produces Native AOT warnings.
    [LibraryImport("d3dcompiler_47.dll", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int D3DCompile(
        void* pSrcData,
        nuint srcDataSize,
        string pSourceName,
        void* pDefines,
        void* pInclude,
        string pEntrypoint,
        string pTarget,
        uint flags1,
        uint flags2,
        ID3D10Blob** ppCode,
        ID3D10Blob** ppErrorMsgs
    );

    public override string Name
    {
        get => _name;
        set
        {
            _name = value;
            D3D11Util.SetDebugName(_deviceShader.Handle, value);
        }
    }

    public override bool IsDisposed => _disposed;

    public override void Dispose()
    {
        if (!_disposed)
        {
            _deviceShader.Dispose();
            _disposed = true;
        }
    }
}
