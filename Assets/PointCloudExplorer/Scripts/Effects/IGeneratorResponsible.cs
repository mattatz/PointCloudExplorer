using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGeneratorResponsible
{

    List<GeneratorBase> Generators { get; set; }
    void Generator(GeneratorBase gen);

}

