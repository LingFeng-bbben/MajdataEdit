/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

namespace MajdataEdit.AutoSaveModule;

/// <summary>
///     自动保存上下文接口
///     接口可以获取自动保存需要的上下文内容，如路径
/// </summary>
internal interface IAutoSaveContext
{
    /// <summary>
    ///     获取保存路径，不包含文件名，结尾没有斜杠
    /// </summary>
    /// <returns></returns>
    string GetSavePath();
}